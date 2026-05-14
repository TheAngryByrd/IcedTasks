(**
---
title: Use and! with independent operations
category: How To Guides
categoryindex: 4
index: 8
---

# How to use `and!` with independent operations

Use `and!` when multiple async operations can start independently and the next step needs all of their results.
This example uses `cancellableTask`, where each operation receives the same `CancellationToken` from the caller.

*)

(*** hide ***)
#r "../../src/IcedTasks/bin/Release/net9.0/IcedTasks.dll"

open System.Threading
open System.Threading.Tasks
open IcedTasks

type UserId = UserId of int

type Profile =
    { UserId: UserId
      DisplayName: string }

type Permissions =
    { CanEdit: bool
      CanExport: bool }

type Preferences =
    { Theme: string }

type NotificationSummary =
    { UnreadCount: int }

type Dashboard =
    { Profile: Profile
      Permissions: Permissions
      Preferences: Preferences
      Notifications: NotificationSummary }

let loadProfile userId : CancellableTask<Profile> =
    cancellableTask {
        let! ct = CancellableTask.getCancellationToken ()
        do! Task.Delay(1, ct)

        let (UserId id) = userId
        return { UserId = userId; DisplayName = $"User {id}" }
    }

let loadPermissions (_userId: UserId) : CancellableTask<Permissions> =
    cancellableTask {
        let! ct = CancellableTask.getCancellationToken ()
        do! Task.Delay(1, ct)
        return { CanEdit = true; CanExport = false }
    }

let loadPreferences (_userId: UserId) : CancellableTask<Preferences> =
    cancellableTask {
        let! ct = CancellableTask.getCancellationToken ()
        do! Task.Delay(1, ct)
        return { Theme = "system" }
    }

let loadNotifications (_userId: UserId) : CancellableTask<NotificationSummary> =
    cancellableTask {
        let! ct = CancellableTask.getCancellationToken ()
        do! Task.Delay(1, ct)
        return { UnreadCount = 3 }
    }

(**
## Start independent operations together

The four loads below do not depend on each other.
`and!` lets the builder start them before awaiting any one result.

*)

let loadDashboard userId : CancellableTask<Dashboard> =
    cancellableTask {
        let! profile = loadProfile userId
        and! permissions = loadPermissions userId
        and! preferences = loadPreferences userId
        and! notifications = loadNotifications userId

        return
            { Profile = profile
              Permissions = permissions
              Preferences = preferences
              Notifications = notifications }
    }

(**
## Keep dependent operations sequential

Use sequential `let!` when the next operation needs a previous result.

*)

let loadDashboardForDisplayName userId : CancellableTask<string * Dashboard> =
    cancellableTask {
        let! profile = loadProfile userId
        let! dashboard = loadDashboard profile.UserId
        return profile.DisplayName, dashboard
    }

(**
## Pass the token at the boundary

The composed operation is still a `CancellableTask<'T>`.
Start it by passing the boundary token.

*)

let dashboard =
    loadDashboard (UserId 42) CancellationToken.None
    |> Async.AwaitTask
    |> Async.RunSynchronously

(**
## Use the same pattern with other builders

The same rule applies across IcedTasks builders that support `and!`: use it for independent operations.
For `cancellableTask`, `cancellableValueTask`, and `coldTask`, the builder starts the independent operands before awaiting either result.
For `task` and `valueTask`, the operands are task-like values, so the work may already be started when the builder combines them.

See [Understanding `and!`](../Explanations/Understanding-and-bang.html) for the builder-by-builder explanation.

*)
