namespace MoneyFox.Infrastructure.DbBackup
{

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Common.Exceptions;
    using Core.DbBackup;
    using Microsoft.Graph;
    using Microsoft.Identity.Client;
    using NLog;
    using Serilog;
    using Logger = NLog.Logger;

    internal class OneDriveService : IOneDriveBackupService
    {
        private const string BACKUP_NAME = "backupmoneyfox3.db";
        private const string BACKUP_NAME_TEMP = "moneyfox.db_upload";
        private const string ARCHIVE_FOLDER_NAME = "Archive";
        private const string BACKUP_ARCHIVE_NAME_TEMPLATE = "backupmoneyfox3_{0}.db";
        private const int BACKUP_ARCHIVE_COUNT = 15;
        private const string ERROR_CODE_CANCELED = "authentication_canceled";

        private readonly IOneDriveAuthenticationService oneDriveAuthenticationService;

        public OneDriveService(IOneDriveAuthenticationService oneDriveAuthenticationService)
        {
            this.oneDriveAuthenticationService = oneDriveAuthenticationService;
        }

        private DriveItem? ArchiveFolder { get; set; }

        public async Task LoginAsync()
        {
            await oneDriveAuthenticationService.CreateServiceClient();
        }

        public async Task LogoutAsync()
        {
            await oneDriveAuthenticationService.LogoutAsync();
        }

        public async Task<bool> UploadAsync(Stream dataToUpload)
        {
            var graphServiceClient = await oneDriveAuthenticationService.CreateServiceClient();
            try
            {
                var uploadedItem = await graphServiceClient.Me.Drive.Special.AppRoot.ItemWithPath(BACKUP_NAME_TEMP)
                    .Content.Request()
                    .PutAsync<DriveItem>(dataToUpload);

                await LoadArchiveFolderAsync(graphServiceClient);
                await DeleteCleanupOldBackupsAsync(graphServiceClient);
                await ArchiveCurrentBackupAsync(graphServiceClient);
                await RenameUploadedBackupAsync(graphServiceClient);

                return uploadedItem != null;
            }
            catch (MsalClientException ex)
            {
                if (ex.ErrorCode == ERROR_CODE_CANCELED)
                {
                    await RestoreArchivedBackupInCaseOfErrorAsync(graphServiceClient);
                    throw new BackupOperationCanceledException(ex);
                }
                await RestoreArchivedBackupInCaseOfErrorAsync(graphServiceClient);
                throw;
            }
            catch (OperationCanceledException ex)
            {
                await RestoreArchivedBackupInCaseOfErrorAsync(graphServiceClient);

                throw new BackupOperationCanceledException(ex);
            }
            catch (Exception ex)
            {
                await RestoreArchivedBackupInCaseOfErrorAsync(graphServiceClient);

                throw new BackupAuthenticationFailedException(ex);
            }
        }

        public async Task<DateTime> GetBackupDateAsync()
        {
            var graphServiceClient = await oneDriveAuthenticationService.CreateServiceClient();
            var existingBackup
                = (await graphServiceClient.Me.Drive.Special.AppRoot.Children.Request().GetAsync()).FirstOrDefault(x => x.Name == BACKUP_NAME);

            if (existingBackup != null)
            {
                return existingBackup.LastModifiedDateTime?.DateTime ?? DateTime.MinValue;
            }

            return DateTime.MinValue;
        }

        public async Task<List<string>> GetFileNamesAsync()
        {
            try
            {
                var graphServiceClient = await oneDriveAuthenticationService.CreateServiceClient();

                return (await graphServiceClient.Me.Drive.Special.AppRoot.Children.Request().GetAsync()).Select(x => x.Name).ToList();
            }
            catch (Exception ex)
            {
                throw new BackupAuthenticationFailedException(ex);
            }
        }

        public async Task<Stream> RestoreAsync()
        {
            try
            {
                var graphServiceClient = await oneDriveAuthenticationService.CreateServiceClient();
                var existingBackup
                    = (await graphServiceClient.Me.Drive.Special.AppRoot.Children.Request().GetAsync()).FirstOrDefault(x => x.Name == BACKUP_NAME);

                if (existingBackup == null)
                {
                    throw new NoBackupFoundException();
                }

                return await graphServiceClient.Drive.Items[existingBackup.Id].Content.Request().GetAsync();
            }
            catch (MsalClientException ex)
            {
                if (ex.ErrorCode == ERROR_CODE_CANCELED)
                {
                    throw new BackupOperationCanceledException();
                }
                throw;
            }
            catch (Exception ex)
            {
                throw new BackupAuthenticationFailedException(ex);
            }
        }

        public async Task<UserAccount> GetUserAccountAsync()
        {
            var graphServiceClient = await oneDriveAuthenticationService.CreateServiceClient();
            var user = await graphServiceClient.Me.Request().GetAsync();

            return new UserAccount(name: user.DisplayName, email: string.IsNullOrEmpty(user.Mail) ? user.UserPrincipalName : user.Mail);
        }

        private async Task RestoreArchivedBackupInCaseOfErrorAsync(GraphServiceClient graphServiceClient)
        {
            try
            {
                var archivedBackups = await graphServiceClient.Drive.Items[ArchiveFolder?.Id].Children.Request().GetAsync();
                if (!archivedBackups.Any())
                {
                    return;
                }

                var lastBackup = archivedBackups.OrderByDescending(x => x.CreatedDateTime).First();
                var appRoot = await graphServiceClient.Me.Drive.Special.AppRoot.Request().GetAsync();
                var updateItem = new DriveItem { ParentReference = new ItemReference { Id = appRoot.Id }, Name = BACKUP_NAME };
                await graphServiceClient.Drive.Items[lastBackup.Id].Request().UpdateAsync(updateItem);
            }
            catch (Exception ex)
            {
                Log.Error(exception: ex, "Failed to restore database on fail");
            }
        }

        private async Task DeleteCleanupOldBackupsAsync(GraphServiceClient graphServiceClient)
        {
            var archiveBackups = await graphServiceClient.Drive.Items[ArchiveFolder?.Id].Children.Request().GetAsync();
            if (archiveBackups.Count < BACKUP_ARCHIVE_COUNT)
            {
                return;
            }

            var oldestBackup = archiveBackups.OrderByDescending(x => x.CreatedDateTime).Last();
            await graphServiceClient.Drive.Items[oldestBackup?.Id].Request().DeleteAsync();
        }

        private async Task ArchiveCurrentBackupAsync(GraphServiceClient graphServiceClient)
        {
            if (ArchiveFolder == null)
            {
                return;
            }

            var currentBackup = (await graphServiceClient.Me.Drive.Special.AppRoot.Children.Request().GetAsync()).FirstOrDefault(x => x.Name == BACKUP_NAME);
            if (currentBackup == null)
            {
                return;
            }

            var updateItem = new DriveItem
            {
                ParentReference = new ItemReference { Id = ArchiveFolder.Id },
                Name = string.Format(
                    provider: CultureInfo.InvariantCulture,
                    format: BACKUP_ARCHIVE_NAME_TEMPLATE,
                    arg0: DateTime.Now.ToString(format: "yyyy-M-d_hh-mm-ssss", provider: CultureInfo.InvariantCulture))
            };

            await graphServiceClient.Drive.Items[currentBackup.Id].Request().UpdateAsync(updateItem);
        }

        private async Task RenameUploadedBackupAsync(GraphServiceClient graphServiceClient)
        {
            var backupUpload
                = (await graphServiceClient.Me.Drive.Special.AppRoot.Children.Request().GetAsync()).FirstOrDefault(x => x.Name == BACKUP_NAME_TEMP);

            if (backupUpload == null)
            {
                return;
            }

            var updateItem = new DriveItem { Name = BACKUP_NAME };
            await graphServiceClient.Drive.Items[backupUpload.Id].Request().UpdateAsync(updateItem);
        }

        private async Task LoadArchiveFolderAsync(GraphServiceClient graphServiceClient)
        {
            if (ArchiveFolder != null)
            {
                return;
            }

            ArchiveFolder
                = (await graphServiceClient.Me.Drive.Special.AppRoot.Children.Request().GetAsync()).CurrentPage.FirstOrDefault(
                    x => x.Name == ARCHIVE_FOLDER_NAME);

            if (ArchiveFolder == null)
            {
                await CreateArchiveFolderAsync(graphServiceClient);
            }
        }

        private async Task CreateArchiveFolderAsync(GraphServiceClient graphServiceClient)
        {
            var folderToCreate = new DriveItem { Name = ARCHIVE_FOLDER_NAME, Folder = new Folder() };
            ArchiveFolder = await graphServiceClient.Me.Drive.Special.AppRoot.Children.Request().AddAsync(folderToCreate);
        }
    }

}
