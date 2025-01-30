# EzLogger Library

EzLogger is an easy to use logging framework for .NET applications designed to
facilitate easy logging and application monitoring with minimal overhead.

## Key Features

### Comprehensive Logging Levels

Five distinct logging levels (`Critical`, `Error`, `Warning`, `Info`, `Debug`)
allow for detailed separation and control over log output, ensuring you receive
just the right amount of information.

### Console and File Logging

Configurable settings for both console and file outputs, enabling consistent
logging to the console and to a log file based on verbosity settings.

### Singleton

Singleton architecture guarantees a single, global logger instance per application,
making logging easy for small applications which do not need complicated logger
interfaces.

### Asynchronous Nature

Logs are collected immediately into a queue and are processed in background while
the main thread continues. Logs are written in batches to the log file to reduce
the amount of write operations, maintaining performance in high-throughput
environments.

### Automated Log Management

Automatically manages log file sizes with a cleanup mechanism that purges old
logs, keeping disk usage in check without manual intervention.

### User-friendly Configuration

Straightforward API for configuring verbosity and other settings, making it easy
to integrate and customize within your application.

### Reliable Operations

Ensures all pending log messages are processed before the application exits,
preventing data loss and ensuring operational integrity.

## Quick Start Guide

### Configuration and Basic Logging

```csharp
using EzLogger;

// Configure logger to debug level for console output and warning level for file output
Logger.SetConfig(Verbosity.Debug, Verbosity.Warning);

// Logging examples
Logger.Debug("Some event has happened.");
Logger.Info("Application initialized successfully.");
Logger.Warning("Low disk space warning.");
Logger.Error("File read error occurred.");
Logger.Critical("Database connection failure!");
```

### Cleanup and Shutdown

```csharp
// Clean up and ensure all logs are flushed before application exit
Logger.StopLoggingTasks();
```

### Example

![Picture](https://i.imgur.com/fvmVpaD.png)

### Installation

Please see the license.

```txt
Restrict You License

Copyright (c) 2024 Usman Mehmood. All rights reserved.

Permission is hereby granted to view the code of this
library for educational purposes only.

No permission is granted to download, use, modify,
distribute, or create derivative works from this code
or any part thereof for any other purposes.
```

## Why Choose EzLogger?

EzLogger is dead simple to use. While it may not be as flexible as other more
advanced loggers, it provides an easy to use interface and is reasonably performant.

### Inner Working

![Flowchart](https://i.imgur.com/nwC9Iu3.png)