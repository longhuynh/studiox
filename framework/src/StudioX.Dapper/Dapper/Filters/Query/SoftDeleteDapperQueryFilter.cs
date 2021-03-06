﻿using System;
using System.Linq.Expressions;
using System.Reflection;

using StudioX.Dapper.Utils;
using StudioX.Domain.Entities;
using StudioX.Domain.Uow;

using DapperExtensions;

namespace StudioX.Dapper.Filters.Query
{
    public class SoftDeleteDapperQueryFilter : IDapperQueryFilter
    {
        private readonly ICurrentUnitOfWorkProvider currentUnitOfWorkProvider;

        public SoftDeleteDapperQueryFilter(ICurrentUnitOfWorkProvider currentUnitOfWorkProvider)
        {
            this.currentUnitOfWorkProvider = currentUnitOfWorkProvider;
        }

        public bool IsDeleted => false;

        public string FilterName { get; } = StudioXDataFilters.SoftDelete;

        public bool IsEnabled => currentUnitOfWorkProvider.Current.IsFilterEnabled(FilterName);

        public IFieldPredicate ExecuteFilter<TEntity, TPrimaryKey>() where TEntity : class, IEntity<TPrimaryKey>
        {
            IFieldPredicate predicate = null;
            if (IsFilterable<TEntity, TPrimaryKey>())
            {
                predicate = Predicates.Field<TEntity>(f => (f as ISoftDelete).IsDeleted, Operator.Eq, IsDeleted);
            }

            return predicate;
        }

        public Expression<Func<TEntity, bool>> ExecuteFilter<TEntity, TPrimaryKey>(Expression<Func<TEntity, bool>> predicate) where TEntity : class, IEntity<TPrimaryKey>
        {
            if (IsFilterable<TEntity, TPrimaryKey>())
            {
                PropertyInfo propType = typeof(TEntity).GetProperty(nameof(ISoftDelete.IsDeleted));
                if (predicate == null)
                {
                    predicate = ExpressionUtils.MakePredicate<TEntity>(nameof(ISoftDelete.IsDeleted), IsDeleted, propType.PropertyType);
                }
                else
                {
                    ParameterExpression paramExpr = predicate.Parameters[0];
                    MemberExpression memberExpr = Expression.Property(paramExpr, nameof(ISoftDelete.IsDeleted));
                    BinaryExpression body = Expression.AndAlso(
                        predicate.Body,
                        Expression.Equal(memberExpr, Expression.Constant(IsDeleted, propType.PropertyType)));
                    predicate = Expression.Lambda<Func<TEntity, bool>>(body, paramExpr);
                }
            }
            return predicate;
        }

        private bool IsFilterable<TEntity, TPrimaryKey>() where TEntity : class, IEntity<TPrimaryKey>
        {
            return typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)) && IsEnabled;
        }
    }
}
