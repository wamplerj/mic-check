using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace MicCheck.Api.Tests.Unit.TestSupport;

public class TestAsyncQueryProvider<T>(IQueryProvider innerProvider) : IAsyncQueryProvider
{
    public IQueryable CreateQuery(Expression expression) =>
        new TestAsyncEnumerable<T>(StripIncludes(expression));

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
        new TestAsyncEnumerable<TElement>(StripIncludes(expression));

    public object? Execute(Expression expression) =>
        innerProvider.Execute(StripIncludes(expression));

    public TResult Execute<TResult>(Expression expression) =>
        innerProvider.Execute<TResult>(StripIncludes(expression));

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var stripped = StripIncludes(expression);
        var resultType = typeof(TResult).GetGenericArguments() is [var inner] ? inner : typeof(TResult);
        var executionResult = typeof(IQueryProvider)
            .GetMethod(nameof(IQueryProvider.Execute), 1, [typeof(Expression)])!
            .MakeGenericMethod(resultType)
            .Invoke(innerProvider, [stripped]);

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, [executionResult])!;
    }

    private static Expression StripIncludes(Expression expression) => new IncludeStripVisitor().Visit(expression);

    private sealed class IncludeStripVisitor : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node) =>
            node.Method.Name is "Include" or "ThenInclude"
                ? Visit(node.Arguments[0])
                : base.VisitMethodCall(node);
    }
}
