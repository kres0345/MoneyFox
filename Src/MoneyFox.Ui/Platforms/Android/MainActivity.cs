namespace MoneyFox.Ui;

using Common;
using Core.Common.Interfaces;
using Core.Interfaces;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using MoneyFox.Infrastructure.DbBackup.Legacy;
using MoneyFox.Ui.Platforms.Android.Resources.Src;

[Activity(Label = "MoneyFox", Theme = "@style/MainTheme", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.UiMode | ConfigChanges.Orientation)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        App.AddPlatformServicesAction = AddServices;
        ParentActivityWrapper.ParentActivity = this;
        base.OnCreate(savedInstanceState);
        Platform.Init(activity: this, bundle: savedInstanceState);
    }

    private static void AddServices(IServiceCollection services)
    {
        services.AddSingleton<IDbPathProvider, DbPathProvider>();
        services.AddSingleton<IStoreOperations, PlayStoreOperations>();
        services.AddSingleton<IAppInformation, DroidAppInformation>();
        services.AddTransient<IFileStore>(_ => new FileStoreIoBase(Application.Context.FilesDir?.Path ?? ""));
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode: requestCode, resultCode: resultCode, data: data);
        AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(requestCode: requestCode, resultCode: resultCode, data: data);
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        Platform.OnRequestPermissionsResult(requestCode: requestCode, permissions: permissions, grantResults: grantResults);
        base.OnRequestPermissionsResult(requestCode: requestCode, permissions: permissions, grantResults: grantResults);
    }
}
