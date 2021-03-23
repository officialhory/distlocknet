# Distributed Lock

General distributed lock solution for .NET

## About

The Distributed Lock can be used to easily and efficently handle locking of different application instances.

### DistLockNet

### ILocker

Events:

- `OnLockAquired(string)`
- `OnLockLost(string)`
- `OnLockFail(string)`
- `OnWaitForUnlock(string)`

Methods:

- `void Lock()` - Starts the locking of the application instance
- `void Halt()` - Releases the lock

### Locker

Implements the ILocker interface.
Creating a Locker instance you are needed to provide the follosing parameters:

- Configuration file
- A backend instance that implements the `ILockingBnd` interface
- A logger instance that implements the `ILogger` interface

### Configuration file

The configuration file that is provided for the Locker should look something like this:

```
{
	"Locker": {
		"ApplicationId": "yourAppId",
		"TimeOutSeconds": timeoutLengthInSeconds,
		"MissedHeartBeatCountBeforeExpire": missedHeartBeatCount
	}
}
```

Timeout must be above 5 seconds, otherwise a `LockerException` is thrown

### ILocingBnd

Methods:

- `Task<LockingObject> GetAsync(string applicationId, CancellationToken ct)` - Returns the LockingObject with the provided application id, or null if it not yet exists in the database
- `Task<bool> AddAsync(LockingObject lo, CancellationToken ct)` - Saves the LockingObject to the database. Returns false if the process fails.
- `Task<bool> AllocateAsync(LockingObject lo, CancellationToken ct)` - If the lock of an other applciation instance is expired the new instance gets the lock. Returns false if the process fails.
- `Task<bool> UpdateAsync(LockingObject lo, CancellationToken ct)` - Refreshes the seed of the LockingObject, signaling that the lock is not yet expired. Returns false if the process fails.

#### LockingObject

Properties:

- `string AppId { get; }` - App to be locked
- `Guid LockerId { get; }` - The id of the lock instance
- `Guid Seed { get; }` - Changes value in every heartbeat

### SqlBackend

Implements the ILockingBnd interface. It uses EF Core 5.0.3 for Db interaction.

### CouchBackend

Implements the ILockingBnd interface. It uses CouchDB 2.3.0 for Db interaction.

## How to use

A compose file can be found at the root of the repository which can be used to set up a Postgres database and a PgAdmin administration and development platform.
All you need to do is to execute the following command from the root of the project: `docker-compse up --build`
When the infrastructure is configured successfully, DistLockTestConsole can be started for testing the solution.
