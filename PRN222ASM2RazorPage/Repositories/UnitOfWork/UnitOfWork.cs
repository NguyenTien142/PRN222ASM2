using Microsoft.EntityFrameworkCore.Storage;
using Repositories.Context;
using Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly Prn222asm2Context _context;
        private readonly IRepositoryFactory _repositoryFactory;
        private IDbContextTransaction? _transaction;

        // Custom repositories (lazy-loaded)
        private IUserRepository? _userRepository;
        private IOrderRepository? _orderRepository;
        private IAppointmentRepository? _appointmentRepository;
        private IDealerRepository? _dealerRepository;
        private IRoleRepository? _roleRepository;
        private IVehicleCategoryRepository? _vehicleCategoryRepository;
        private ICustomerRepository? _customerRepository;
        private IVehicleRepository? _vehicleRepository;


        public UnitOfWork(Prn222asm2Context context, IRepositoryFactory repositoryFactory)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
        }

        #region Custom Repositories
        public IUserRepository UserRepository =>
            _userRepository ??= _repositoryFactory.GetCustomRepository<IUserRepository>();

        public IOrderRepository OrderRepository =>
            _orderRepository ??= _repositoryFactory.GetCustomRepository<IOrderRepository>();

        public IAppointmentRepository AppointmentRepository =>
            _appointmentRepository ??= _repositoryFactory.GetCustomRepository<IAppointmentRepository>();

        public IDealerRepository DealerRepository =>
            _dealerRepository ??= _repositoryFactory.GetCustomRepository<IDealerRepository>();

        public IRoleRepository RoleRepository =>
            _roleRepository ??= _repositoryFactory.GetCustomRepository<IRoleRepository>();

        public IVehicleCategoryRepository VehicleCategoryRepository =>
            _vehicleCategoryRepository ??= _repositoryFactory.GetCustomRepository<IVehicleCategoryRepository>();

        public ICustomerRepository CustomerRepository =>
            _customerRepository ??= _repositoryFactory.GetCustomRepository<ICustomerRepository>();

        public IVehicleRepository VehicleRepository =>
            _vehicleRepository ??= _repositoryFactory.GetCustomRepository<IVehicleRepository>();

        #endregion

        #region Generic Repository
        public IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>()
            where TEntity : class
        {
            return _repositoryFactory.GetRepository<TEntity, TKey>();
        }
        #endregion

        #region Save & Transactions
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync()
        {
            _transaction ??= await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
                await _transaction.DisposeAsync();

            if (_context != null)
                await _context.DisposeAsync();
        }
        #endregion
    }
}
