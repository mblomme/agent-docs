namespace Company.BoundedContext.DesignTests.Common;

internal sealed record EqualityExample(
    Func<object> First,
    Func<object> Equal,
    Func<object> Different
);
