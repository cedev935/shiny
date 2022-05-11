﻿using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny;


public static class RxExtensions
{
    /// <summary>
    /// Equivalent of switchMap in RXJS
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="U"></typeparam>
    /// <param name="current"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    public static IObservable<U> SelectSwitch<T, U>(this IObservable<T> current, Func<T, IObservable<U>> next) 
        => current.Select(x => next(x)).Switch();


    /// <summary>
    /// Quick helper method to execute an async select
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="U"></typeparam>
    /// <param name="observable"></param>
    /// <param name="task"></param>
    /// <returns></returns>
    public static IObservable<U> SelectAsync<T, U>(this IObservable<T> observable, Func<Task<U>> task)
        => observable.Select(x => Observable.FromAsync(() => task())).Switch();


    // TODO: combineLatest?
    /// <summary>
    /// Passes the last and current values from the stream
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ob"></param>
    /// <returns></returns>
    public static IObservable<Tuple<T, T>> WithPrevious<T>(this IObservable<T> ob)
        => ob.Scan(Tuple.Create(default(T), default(T)), (acc, current) => Tuple.Create(acc.Item2, current));


  


    /// <summary>
    /// Quick helper method to execute an async select
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="U"></typeparam>
    /// <param name="observable"></param>
    /// <param name="task"></param>
    /// <returns></returns>
    public static IObservable<U> SelectAsync<T, U>(this IObservable<T> observable, Func<CancellationToken, Task<U>> task)
        => observable.Select(x => Observable.FromAsync(ct => task(ct))).Switch();


    /// <summary>
    /// This is to make chaining easier when a scheduler is null
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ob"></param>
    /// <param name="scheduler"></param>
    /// <returns></returns>
    public static IObservable<T> ObserveOnIf<T>(this IObservable<T> ob, IScheduler? scheduler)
    {
        if (scheduler != null)
            ob.ObserveOn(scheduler);

        return ob;
    }


    ///// <summary>
    ///// Warning - this will block until completed
    ///// </summary>
    ///// <typeparam name="T"></typeparam>
    ///// <param name="source"></param>
    ///// <param name="onFinally"></param>
    ///// <param name="timeout"></param>
    ///// <returns></returns>
    //public static IObservable<T> DoFinally<T>(this IObservable<T> source, IObservable<Unit> onFinally, TimeSpan? timeout = null) => source.Finally(() =>
    //{
    //    var mre = new ManualResetEvent(false);
    //    onFinally
    //        .SubscribeOn(ThreadPoolScheduler.Instance) // hmmmm.... hmmmmmmmmmmmmmmmmmm...
    //        .Subscribe(
    //            _ => mre.Set(), // there is only one take - I don't care if it completes or not - 1 and done
    //            _ => mre.Set()  // if erroring - also done
    //        );

    //    mre.WaitOne(timeout ?? TimeSpan.FromSeconds(5));
    //});


    /// <summary>
    /// Runs an action only once when the first result is received
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obs"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public static IObservable<T> DoOnce<T>(this IObservable<T> obs, Action<T> action)
    {
        var count = 1;
        return obs.Do(x =>
        {
            if (count == 0)
            {
                Interlocked.Increment(ref count);
                action(x);
            }
        });
    }


    /// <summary>
    /// A function from ReactiveUI - useful for non-ui stuff too ;)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="this"></param>
    /// <param name="compositeDisposable"></param>
    /// <returns></returns>
    public static T DisposedBy<T>(this T @this, CompositeDisposable compositeDisposable) where T : IDisposable
    {
        compositeDisposable.Add(@this);
        return @this;
    }


    /// <summary>
    /// A handy way for replying and completing an observer - common for single valued observables
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ob"></param>
    /// <param name="value"></param>
    public static void Respond<T>(this IObserver<T> ob, T value)
    {
        ob.OnNext(value);
        ob.OnCompleted();
    }


    /// <summary>
    /// Async Subscribe done properly
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="observable"></param>
    /// <param name="onNextAsync"></param>
    /// <returns></returns>
    public static IDisposable SubscribeAsync<T>(this IObservable<T> observable, Func<T, Task> onNextAsync)
        => observable
            .Select(x => Observable.FromAsync(() => onNextAsync(x)))
            .Concat()
            .Subscribe();


    /// <summary>
    /// Async Subscribe done properly
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="observable"></param>
    /// <param name="onNextAsync"></param>
    /// <param name="onError"></param>
    /// <returns></returns>
    public static IDisposable SubscribeAsync<T>(this IObservable<T> observable,
                                                Func<T, Task> onNextAsync,
                                                Action<Exception> onError)
        => observable
            .Select(x => Observable.FromAsync(() => onNextAsync(x)))
            .Concat()
            .Subscribe(
                _ => { },
                onError
            );


    /// <summary>
    /// Async Subscribe done properly
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="observable"></param>
    /// <param name="onNextAsync"></param>
    /// <param name="onError"></param>
    /// <param name="onComplete"></param>
    /// <returns></returns>
    public static IDisposable SubscribeAsync<T>(this IObservable<T> observable,
                                                Func<T, Task> onNextAsync,
                                                Action<Exception> onError,
                                                Action onComplete)
        => observable
            .Select(x => Observable.FromAsync(() => onNextAsync(x)))
            .Concat()
            .Subscribe(
                _ => { },
                onError,
                onComplete
            );

    /// <summary>
    /// Async subscribe done properly while also ensuring that only async value runs at a time
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="observable"></param>
    /// <param name="onNextAsync"></param>
    /// <returns></returns>
    public static IDisposable SubscribeAsyncConcurrent<T>(this IObservable<T> observable, Func<T, Task> onNextAsync)
        => observable
            .Select(x => Observable.FromAsync(() => onNextAsync(x)))
            .Merge()
            .Subscribe();


    /// <summary>
    /// Async subscribe done properly while also ensuring that only async value runs at a time
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="observable"></param>
    /// <param name="onNextAsync"></param>
    /// <param name="maxConcurrent"></param>
    /// <returns></returns>
    public static IDisposable SubscribeAsyncConcurrent<T>(this IObservable<T> observable, Func<T, Task> onNextAsync, int maxConcurrent)
        => observable
            .Select(x => Observable.FromAsync(() => onNextAsync(x)))
            .Merge(maxConcurrent)
            .Subscribe();
}