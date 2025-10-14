using Microsoft.EntityFrameworkCore;
using Repositories.Context;
using Repositories.Helpper;
using Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.GenericRepository
{
    public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey> where TEntity : class
    {
        private readonly Prn222asm2Context _context;
        private readonly DbSet<TEntity> _dbSet;

        public GenericRepository(Prn222asm2Context context)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
        }

        public async Task<IReadOnlyList<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>>? predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet;

            if (predicate != null) query = query.Where(predicate);

            foreach (var include in includes)
                query = query.Include(include);

            if (orderBy != null) query = orderBy(query);

            return await query.ToListAsync();
        }

        public async Task<TEntity?> GetByIdAsync(TKey id, params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet;

            foreach (var include in includes)
                query = query.Include(include);

            return await query.FirstOrDefaultAsync(e => EF.Property<TKey>(e, "Id")!.Equals(id));
        }

        public async Task<TEntity?> FirstOrDefaultAsync(
            Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet;

            foreach (var include in includes)
                query = query.Include(include);

            return await query.FirstOrDefaultAsync(predicate);
        }

        public async Task<PagedResult<TEntity>> GetPagedAsync(
            int pageIndex, int pageSize,
            Expression<Func<TEntity, bool>>? predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet;

            if (predicate != null) query = query.Where(predicate);

            foreach (var include in includes)
                query = query.Include(include);

            int totalCount = await query.CountAsync();

            if (orderBy != null) query = orderBy(query);

            var items = await query.Skip((pageIndex - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            return new PagedResult<TEntity>(items, totalCount, pageIndex, pageSize);
        }

        public async Task<TEntity> AddAsync(TEntity entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            await _dbSet.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        public async Task<TEntity> UpdateAsync(TEntity entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateRangeAsync(IEnumerable<TEntity> entities)
        {
            _dbSet.UpdateRange(entities);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(TKey id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null) return false;

            _dbSet.Remove(entity);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> SoftDeleteAsync(TKey id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null) return false;

            // Check for IsDeleted property (used by User, Product, Category entities)
            var isDeletedProperty = entity.GetType().GetProperty("IsDeleted");
            if (isDeletedProperty != null && isDeletedProperty.PropertyType == typeof(bool))
            {
                isDeletedProperty.SetValue(entity, true);
                _dbSet.Update(entity);
                return await _context.SaveChangesAsync() > 0;
            }

            // Check for IsDelete property (alternative naming)
            var isDeleteProperty = entity.GetType().GetProperty("IsDelete");
            if (isDeleteProperty != null && isDeleteProperty.PropertyType == typeof(bool?))
            {
                isDeleteProperty.SetValue(entity, true);
                _dbSet.Update(entity);
                return await _context.SaveChangesAsync() > 0;
            }

            return false; // Entity doesn't support soft delete
        }

        public async Task DeleteRangeAsync(IEnumerable<TKey> ids)
        {
            var entities = await _dbSet
                .Where(e => ids.Contains(EF.Property<TKey>(e, "Id")))
                .ToListAsync();

            _dbSet.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }
    }
}
