namespace MoneyFox.Core.Features._Legacy_.Categories.DeleteCategoryById;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class DeleteCategoryByIdCommand : IRequest
{
    public DeleteCategoryByIdCommand(int categoryId)
    {
        CategoryId = categoryId;
    }

    public int CategoryId { get; }

    public class Handler : IRequestHandler<DeleteCategoryByIdCommand>
    {
        private readonly IAppDbContext dbContext;

        public Handler(IAppDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<Unit> Handle(DeleteCategoryByIdCommand command, CancellationToken cancellationToken)
        {
            var paymentsWithCategory = await dbContext.Payments.Include(p => p.Category)
                .Where(p => p.Category != null)
                .Where(p => p.Category!.Id == command.CategoryId)
                .ToListAsync(cancellationToken);

            paymentsWithCategory.ForEach(p => p.RemoveCategory());

            var entityToDelete = await dbContext.Categories.FindAsync(command.CategoryId);
            if (entityToDelete is null)
            {
                return Unit.Value;
            }

            dbContext.Categories.Remove(entityToDelete);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
