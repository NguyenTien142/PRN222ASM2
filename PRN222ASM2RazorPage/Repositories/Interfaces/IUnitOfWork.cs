using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        // Generic repository
        IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>()
            where TEntity : class;

        // Custom repositories
        IUserRepository UserRepository { get; }
        IOrderRepository OrderRepository { get; }
        IAppointmentRepository AppointmentRepository { get; }
        IDealerRepository DealerRepository { get; }
        IRoleRepository RoleRepository { get; }
        IVehicleCategoryRepository VehicleCategoryRepository { get; }
        ICustomerRepository CustomerRepository { get; }
        IVehicleRepository VehicleRepository { get; }

        // Save changes
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        // Transactions
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
