open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe


module Database =
    open Dapper
    open System.Data
    open System.Threading
    open IcedTasks

    let connStr =
        Npgsql.NpgsqlConnectionStringBuilder(
            Host = "localhost",
            Port = 5432,
            Username = "postgres",
            Password = "postgres"
        )

    let doSlowWorkAsync (conn: IDbConnection) =
        async {
            let! ct = Async.CancellationToken
            let cmdDef = CommandDefinition("select pg_sleep(10)", cancellationToken = ct)

            let! _ =
                conn.QueryAsync(cmdDef)
                |> Async.AwaitTask

            ()
        }

    let doSlowWork1 (conn: IDbConnection) =
        task {
            let! _ = conn.QueryAsync("select pg_sleep(10)")
            ()
        }

    let doSlowWork2 (ct: CancellationToken) (conn: IDbConnection) =
        task {
            let cmdDef = CommandDefinition("select pg_sleep(10)", cancellationToken = ct)
            let! _ = conn.QueryAsync(cmdDef)
            ()
        }

    // CancellableTask<'T> = CancellationToken -> Task<'T>
    let doSlowWork3 (conn: IDbConnection) =
        cancellableTask {
            let! ct = CancellableTask.getCancellationToken ()
            let cmdDef = CommandDefinition("select pg_sleep(10)", cancellationToken = ct)
            let! _ = conn.QueryAsync(cmdDef)
            ()
        }
    // CancellableTask<'T> = CancellationToken -> Task<'T>
    let doSlowWork3b (conn: IDbConnection) =
        cancellableTask {
            let! _ =
                fun ct ->
                    let cmdDef = CommandDefinition("select pg_sleep(10)", cancellationToken = ct)
                    conn.QueryAsync(cmdDef)

            ()
        }

open Database

let webApp =
    choose [
        // This is not using CancellationTokens
        route "/simpleExample1"
        >=> fun next ctx ->
            task {
                use conn = new Npgsql.NpgsqlConnection(connStr.ToString())
                do! doSlowWork1 (conn)
                return! next ctx
            }

        // This is using CancellationTokens explicitly
        route "/simpleExample2"
        >=> fun next ctx ->
            task {
                use conn = new Npgsql.NpgsqlConnection(connStr.ToString())
                do! doSlowWork2 ctx.RequestAborted conn
                return! next ctx
            }


        // This is using CancellationTokens at the top level and only when needed
        route "/simpleExample3"
        >=> fun next ctx ->
            task {
                use conn = new Npgsql.NpgsqlConnection(connStr.ToString())
                do! doSlowWork3 conn ctx.RequestAborted
                return! next ctx
            }
    ]

module BusinessLogic =
    open System.Threading.Tasks
    open Microsoft.Extensions.Caching.Distributed

    let preserveEndocrins (logger: ILogger) () = task { do! Task.Delay(100) }

    let monotonectallyImplementErrorFreeConvergence (logger: ILogger) id =
        task {
            let! _ = preserveEndocrins logger ()
            return id
        }

    let fungiblyFacilitateTechnicallySoundResults id conn =
        task {
            let! _ = Database.doSlowWork1 conn
            ()
        }

    let completelyTransitionBackendRelationships () =
        task {
            //send an email?
            ()
        }

    let continuallyActualizeImperatives (logger: ILogger) (conn) =
        task {
            for i = 0 to 100000 do
                let! r1 = monotonectallyImplementErrorFreeConvergence logger i
                let! _ = fungiblyFacilitateTechnicallySoundResults r1 conn
                ()
        }

    let reticulatingSplines (logger: ILogger) (caching: IDistributedCache) (conn) =
        task {
            logger.LogDebug("Started reticulatingSplines")
            let! _ = preserveEndocrins logger ()
            logger.LogInformation("preserveEndocrins might be doing something strange?")
            let! _ = Database.doSlowWork1 conn
            let! _ = continuallyActualizeImperatives logger conn
            let! _ = completelyTransitionBackendRelationships ()
            ()
        }


module BusinessLogic2 =
    open System.Threading
    open System.Threading.Tasks
    open Microsoft.Extensions.Caching.Distributed

    let preserveEndocrins (logger: ILogger) (ct: CancellationToken) () =
        task { do! Task.Delay(100, ct) }

    let monotonectallyImplementErrorFreeConvergence (ct: CancellationToken) (logger: ILogger) id =
        task {
            let! _ = preserveEndocrins logger ct ()
            return id
        }

    let fungiblyFacilitateTechnicallySoundResults id (ct: CancellationToken) conn =
        task {
            let! _ = Database.doSlowWork2 ct conn
            ()
        }

    let completelyTransitionBackendRelationships (ct: CancellationToken) () =
        task {
            //send an email?
            ()
        }

    let continuallyActualizeImperatives (logger: ILogger) (ct: CancellationToken) (conn) =
        task {
            for i = 0 to 100000 do
                let! r1 = monotonectallyImplementErrorFreeConvergence ct logger i
                let! _ = fungiblyFacilitateTechnicallySoundResults r1 ct conn
                ()
        }

    let reticulatingSplines
        (logger: ILogger)
        (caching: IDistributedCache)
        (ct: CancellationToken)
        (conn)
        =
        task {
            logger.LogDebug("Started reticulatingSplines")
            let! _ = preserveEndocrins logger ct ()
            logger.LogInformation("preserveEndocrins might be doing something strange?")
            let! _ = Database.doSlowWork2 ct conn
            let! _ = continuallyActualizeImperatives logger ct conn
            let! _ = completelyTransitionBackendRelationships ct ()
            ()
        }


module BusinessLogic3 =
    open System.Threading
    open System.Threading.Tasks
    open Microsoft.Extensions.Caching.Distributed
    open IcedTasks

    let preserveEndocrins (logger: ILogger) () =
        cancellableTask {
            // You can bind against `CancellationToken -> Task<'T>` calls
            // no need for `CancellableTask.getCancellationToken()`.
            do! fun ct -> Task.Delay(100, ct)
        }

    let monotonectallyImplementErrorFreeConvergence (logger: ILogger) id =
        cancellableTask {
            let! _ = preserveEndocrins logger ()
            return id
        }

    let fungiblyFacilitateTechnicallySoundResults id conn =
        cancellableTask {
            let! _ = Database.doSlowWork3 conn
            ()
        }

    let completelyTransitionBackendRelationships () =
        cancellableTask {
            //send an email?
            ()
        }

    let continuallyActualizeImperatives (logger: ILogger) (conn) =
        cancellableTask {
            for i = 0 to 100000 do
                let! r1 = monotonectallyImplementErrorFreeConvergence logger i
                let! _ = fungiblyFacilitateTechnicallySoundResults r1 conn
                ()
        }

    let reticulatingSplines (logger: ILogger) (caching: IDistributedCache) (conn) =
        cancellableTask {
            logger.LogDebug("Started reticulatingSplines")
            let! _ = preserveEndocrins logger ()
            logger.LogInformation("preserveEndocrins might be doing something strange?")
            let! _ = Database.doSlowWork3 conn
            let! _ = continuallyActualizeImperatives logger conn
            let! _ = completelyTransitionBackendRelationships ()
            ()
        }

type Startup() =
    member __.ConfigureServices(services: IServiceCollection) =
        // Register default Giraffe dependencies
        services.AddGiraffe()
        |> ignore

    member __.Configure
        (app: IApplicationBuilder)
        (env: IHostEnvironment)
        (loggerFactory: ILoggerFactory)
        =
        // Add Giraffe to the ASP.NET Core pipeline
        app.UseGiraffe webApp

[<EntryPoint>]
let main _ =
    Host
        .CreateDefaultBuilder()
        .ConfigureWebHostDefaults(fun webHostBuilder ->
            webHostBuilder.UseStartup<Startup>()
            |> ignore
        )
        .Build()
        .Run()

    0
