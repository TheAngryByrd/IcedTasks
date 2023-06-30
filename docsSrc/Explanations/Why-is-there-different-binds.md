---
title: Why is there different binds
category: Explanations
categoryindex: 3
index: 2
---

# Why is there different binds?

> Here be dragons


You may have browsed around the F# codebase for [Resumable Tasks](https://github.com/dotnet/fsharp/blob/b9685e8f62bb573adec221cce7e62a71f7f430f4/src/FSharp.Core/tasks.fs#L335) or this one have noticed this `Bind` method:


```fsharp
[<NoEagerConstraintApplication>]
member inline _.Bind< ^TaskLike, 'TResult1, 'TResult2, ^Awaiter, 'TOverall
    when ^TaskLike: (member GetAwaiter: unit -> ^Awaiter)
    and ^Awaiter :> ICriticalNotifyCompletion
    and ^Awaiter: (member get_IsCompleted: unit -> bool)
    and ^Awaiter: (member GetResult: unit -> 'TResult1)>
    (
        task: ^TaskLike,
        continuation: ('TResult1 -> TaskCode<'TOverall, 'TResult2>)
    ) : TaskCode<'TOverall, 'TResult2> =
```

What is going on here? Well to understand this, we need to understand a few other concepts first.

In programming, there's something called [duck typing](https://en.wikipedia.org/wiki/Duck_typing). Essentially if an object fits a certain shape, methods can be called. For instance, you don't need to implement IEnumerator on an object to use it in a foreach loop, you just need to implement the methods that `IEnumerable` requires, such as `Current` and `MoveNext()`

See this very good article describing this in detail. [Things you might not know about CSharp - Duck Typing](https://im5tu.io/article/2022/01/things-you-might-not-know-about-csharp-duck-typing/) for a bit more details.

This is also true of async/await. As long as an object has `GetAwaiter` (known as an `Awaitable`), and returns an `Awaiter` which subsequently implements `INotifyCompletion` and has members `IsCompleted`, and `OnCompleted`, it can be used in an async method. (See article above for more details). But ok, why is this important? It's because this creates parity with C#.  The most common example of an `Awaitable` that isn't a `Task/Task<T>/ValueTask/ValueTask<T>` is `Task.Yield()` which returns a `YieldAwaitable`. 

Ok so how does this relate to the previous `Bind` method.

To handle duck typing F# has this concept known as [Statically Resolved Type Parameters](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/generics/statically-resolved-type-parameters), allowing you to get shapes of objects at compile time.  This allows for use to use that `Awaitable/Awaiter` concepts in the generic sense. This means as long as it fits the shape, like in the case of `Task.Yield()` it will work. Awesome!

However when you go to use a `Task<T>` you get this error message:

```
A unique overload for method 'GetAwaiter' could not be determined based on type information prior to this program point. A type annotation may be needed.

Known return type: Awaiter<^a,'b>

Candidates:
 - Task.GetAwaiter() : TaskAwaiter
 - Task.GetAwaiter() : TaskAwaiter<'TResult1>F# Compiler43
```

Why is this? Because `Task<T>` implements two `GetAwaiter` methods and the compiler can't figure out which one to use (one for the generic and non generic form of `Task<T> `and `Task`.  So we need to create an overload that takes that `Task<T>`  specifically. `ValueTask<T>` don't suffer from this as it was designed not to implement both the generic and non generic form. See this issue [Provide conversion from ValueTask<T> to ValueTask](https://github.com/dotnet/runtime/issues/31503#issuecomment-554415966)

So we have this overload:

```fsharp
member inline _.Bind
    (
        task: Task<'TResult1>,
        continuation: ('TResult1 -> TaskCode<'TOverall, 'TResult2>)
    ) : TaskCode<'TOverall, 'TResult2> =
```

In IcedTasks we use `Source` member for this.


```fsharp
member inline _.Source(task: Task<'T>) =
    (fun (ct: CancellationToken) -> task.GetAwaiter())

```

This is a very underdocumented feature of F# Computation Expressions. The best docs on it are on this StackOverflow post [Why would you use Builder.Source() in a custom computation expression builder?](https://stackoverflow.com/questions/35286541/why-would-you-use-builder-source-in-a-custom-computation-expression-builder). But this lets us not have to repeat `Bind`/`ReturnFrom` members for every type we want to support.




