﻿namespace MoneyFox.Core.Queries
{

    using System.Threading;
    using System.Threading.Tasks;
    using Aggregates;
    using Common.Interfaces;
    using MediatR;

    public class GetAccountByIdQuery : IRequest<Account>
    {
        public GetAccountByIdQuery(int accountId)
        {
            AccountId = accountId;
        }

        public int AccountId { get; }

        public class Handler : IRequestHandler<GetAccountByIdQuery, Account>
        {
            private readonly IContextAdapter contextAdapter;

            public Handler(IContextAdapter contextAdapter)
            {
                this.contextAdapter = contextAdapter;
            }

            public async Task<Account> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
            {
                return await contextAdapter.Context.Accounts.FindAsync(request.AccountId);
            }
        }
    }

}