using System.Linq.Expressions;
using MicCheck.Api.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MicCheck.Api.Tests.Unit.TestSupport;

public static class MockDbSetFactory
{
    public static Mock<DbSet<T>> Create<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(() => new TestAsyncEnumerator<T>(data.GetEnumerator()));

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(() => new TestAsyncQueryProvider<T>(data.AsQueryable().Provider));
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(() => data.AsQueryable().Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => data.GetEnumerator());

        mockSet.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(item => data.Add(item));
        mockSet.Setup(m => m.AddRange(It.IsAny<IEnumerable<T>>())).Callback<IEnumerable<T>>(items => data.AddRange(items));
        mockSet.Setup(m => m.Remove(It.IsAny<T>())).Callback<T>(item => data.Remove(item));
        mockSet.Setup(m => m.RemoveRange(It.IsAny<IEnumerable<T>>())).Callback<IEnumerable<T>>(items =>
        {
            foreach (var item in items.ToList())
                data.Remove(item);
        });

        return mockSet;
    }

    public static void SetupDbSet<T>(this Mock<IMicCheckDbContext> context, Expression<Func<IMicCheckDbContext, DbSet<T>>> selector, List<T> data)
        where T : class =>
        context.Setup(selector).Returns(Create(data).Object);

    /// <summary>
    /// Sets up a DbSet whose Add() simulates database-generated identity assignment (needed because
    /// Moq does not run SaveChanges and entity Id properties are commonly init-only).
    /// </summary>
    public static void SetupDbSetWithGeneratedIds<T>(this Mock<IMicCheckDbContext> context, Expression<Func<IMicCheckDbContext, DbSet<T>>> selector, List<T> data)
        where T : class
    {
        var idProperty = typeof(T).GetProperty("Id") ?? throw new InvalidOperationException($"{typeof(T).Name} has no Id property.");
        var mockSet = Create(data);
        mockSet.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(item =>
        {
            idProperty.SetValue(item, data.Count + 1);
            data.Add(item);
        });
        context.Setup(selector).Returns(mockSet.Object);
    }
}
