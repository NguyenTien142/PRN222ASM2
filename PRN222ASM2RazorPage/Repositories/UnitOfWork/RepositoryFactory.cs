using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Repositories.Context;
using Repositories.GenericRepository;
using Repositories.Helpper;
using Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.UnitOfWork
{
    public class RepositoryFactory : IRepositoryFactory
    {
        private readonly Prn222asm2Context _context;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, object> _repositories = new();

        public RepositoryFactory(Prn222asm2Context context, IServiceProvider serviceProvider)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>()
            where TEntity : class
        {
            var type = typeof(IGenericRepository<TEntity, TKey>);

            if (_repositories.TryGetValue(type, out var repo))
            {
                return (IGenericRepository<TEntity, TKey>)repo;
            }

            var repository = new GenericRepository<TEntity, TKey>(_context);
            _repositories[type] = repository;

            return repository;
        }

        public TRepository GetCustomRepository<TRepository>()
            where TRepository : class
        {
            var type = typeof(TRepository);

            if (_repositories.TryGetValue(type, out var repo))
            {
                return (TRepository)repo;
            }

            var repository = _serviceProvider.GetRequiredService<TRepository>();
            _repositories[type] = repository;

            return repository;
        }
    }
}
