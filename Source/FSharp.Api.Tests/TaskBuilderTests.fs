﻿module Orleankka.FSharp.TaskBuilderTests

open NUnit.Framework
open System.Threading
open System.Threading.Tasks

[<Test>]
let ``task should return the right value after let!``() =
    let task = Task.TaskBuilder()
    let t() = 
        task {
            let! v = Task.Factory.StartNew(fun () -> 100)
            return v
        }

    match Task.run t with
    | Task.Canceled -> Assert.Fail("Task should have been successful, but was canceled")
    | Task.Error e -> Assert.Fail("Task should have been successful, but errored with exception {0}", e)
    | Task.Successful a -> Assert.AreEqual(100,a)


[<Test>]
let ``task should return the right value after return!``() =
    let task = Task.TaskBuilder()
    let t() = 
        task {
            return! Task.Factory.StartNew(fun () -> "hello world")
        }

    match Task.run t with
    | Task.Canceled -> Assert.Fail("Task should have been successful, but was canceled")
    | Task.Error e -> Assert.Fail("Task should have been successful, but errored with exception {0}", e)
    | Task.Successful a -> Assert.AreEqual("hello world",a)


[<Test>]
let ``exception in task``() =    
    let task = Task.TaskBuilder(continuationOptions = TaskContinuationOptions.ExecuteSynchronously)
    let t() = 
        task {
            failwith "error"
        }
    match Task.run t with
    | Task.Error e -> Assert.AreEqual("error", e.Message)
    | _ -> Assert.Fail "task should have errored"

[<Test>]
let ``canceled task``() =
    use cts = new CancellationTokenSource()
    let task = Task.TaskBuilder(cancellationToken = cts.Token)
    let t() = 
        task {
            cts.Token.ThrowIfCancellationRequested()
        }
    cts.Cancel()
    match Task.run t with
    | Task.Canceled -> ()
    | Task.Error e -> Assert.Fail("Task should have been canceled, but errored with exception {0}", e)
    | Task.Successful a -> Assert.Fail("Task should have been canceled, but succeeded with result {0}", a)

[<Test>]
let ``canceled task 2``() =
    use cts = new CancellationTokenSource()
    let task = Task.TaskBuilder(cancellationToken = cts.Token)
    let t() = 
        task {
            let! v = Task.Factory.StartNew(fun () -> 0)
            return ()
        }
    cts.Cancel()
    match Task.run t with
    | Task.Canceled -> ()
    | Task.Error e -> Assert.Fail("Task should have been canceled, but errored with exception {0}", e)
    | Task.Successful a -> Assert.Fail("Task should have been canceled, but succeeded with result {0}", a)

[<Test>]
let ``while``() = 
    let i = ref 10    
    let task = Task.TaskBuilder(continuationOptions = TaskContinuationOptions.ExecuteSynchronously)
    let t() =
        task {
            while !i > 0 do
                decr i
                do! Task.Factory.StartNew ignore
        }
    Task.run t |> ignore
    Assert.AreEqual(0, !i)

[<Test>]
let ``try with should catch exception in the body``() =
   let task = Task.TaskBuilder(continuationOptions = TaskContinuationOptions.ExecuteSynchronously)
   let result = task {
      try 
         failwith "exception"
         return 1
      with e -> return 5
   }
   Assert.AreEqual(5, result.Result)

[<Test>]
let ``try with should catch exception in the continuation``() =
   let task = Task.TaskBuilder(continuationOptions = TaskContinuationOptions.ExecuteSynchronously)
   let result = task {
      try 
         do! Task.Factory.StartNew(fun () -> failwith "exception")            
         return 1
      with e -> return 5
   }
   Assert.AreEqual(5, result.Result)

[<Test>]
let ``try with should catch exception only by type``() =
   let task = Task.TaskBuilder(continuationOptions = TaskContinuationOptions.ExecuteSynchronously)
   let result = task {
      try 
         invalidArg "param name" "msg"
         return 1
      with                   
      | :? System.NullReferenceException -> return 5
      | :? System.ArgumentException -> return 10
      | e -> return 15
   }
   Assert.AreEqual(10, result.Result)

[<Test>]
let ``try with should do unwrapping of exception to original type if it was raised in continuation``() =
   let task = Task.TaskBuilder(continuationOptions = TaskContinuationOptions.ExecuteSynchronously)
   let result = task {
      try 
         do! Task.Factory.StartNew(fun () -> invalidArg "param name" "msg")
         return 1
      with
      | :? System.NullReferenceException -> return 5
      | :? System.ArgumentException -> return 10
      | e -> return 15
   }
   Assert.AreEqual(10, result.Result)