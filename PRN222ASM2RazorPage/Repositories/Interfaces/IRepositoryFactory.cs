using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IRepositoryFactory
    {
        /// <summary>
        /// Get a generic repository for any entity type
        /// </summary>
        IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>()
            where TEntity : class;

        /// <summary>
        /// Get a custom repository that is registered in DI container
        /// </summary>
        TRepository GetCustomRepository<TRepository>()
            where TRepository : class;
    }
}
