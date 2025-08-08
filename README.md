# ReactiveMarbles.Command

A reactive command library for C# applications that encapsulates user actions behind a reactive interface. This library provides a modern, reactive approach to handling commands in user interface applications using Reactive Extensions (Rx).

## Installation

```bash
dotnet add package ReactiveMarbles.Command
```

## Overview

ReactiveMarbles.Command provides:
- **RxCommand**: A reactive implementation of ICommand
- **Asynchronous execution**: Built-in support for async operations
- **Execution state tracking**: Monitor when commands are executing
- **Error handling**: Reactive error handling with ThrownExceptions observable
- **Cancellation support**: Built-in cancellation token support
- **Can execute logic**: Reactive can execute conditions

## Basic Usage

### Simple Synchronous Command

```csharp
using ReactiveMarbles.Command;
using System.Reactive;

// Create a simple command that executes immediately
var saveCommand = RxCommand.Create(() => 
{
    Console.WriteLine("Data saved!");
});

// Execute the command
saveCommand.Execute().Subscribe();
```

### Command with Parameter

```csharp
// Create a command that takes a parameter
var deleteCommand = RxCommand.Create<string>(fileName => 
{
    Console.WriteLine($"Deleting file: {fileName}");
});

// Execute with parameter
deleteCommand.Execute("document.txt").Subscribe();
```

### Asynchronous Command

```csharp
// Create an async command
var loadDataCommand = RxCommand.Create(async () => 
{
    await Task.Delay(2000); // Simulate network call
    Console.WriteLine("Data loaded!");
});

// Execute and subscribe to results
loadDataCommand.Execute().Subscribe();
```

### Command with Return Value

```csharp
// Create a command that returns a value
var calculateCommand = RxCommand.Create<int, string>(number => 
{
    var result = number * 2;
    return $"Result: {result}";
});

// Execute and handle the result
calculateCommand.Execute(5).Subscribe(result => 
{
    Console.WriteLine(result); // Output: "Result: 10"
});
```

## WPF/MVVM Example

Here's how to use ReactiveMarbles.Command in a WPF MVVM application:

### ViewModel Implementation

```csharp
using ReactiveMarbles.Command;
using ReactiveMarbles.Mvvm;
using System.Reactive;
using System.Reactive.Linq;

public class MainViewModel : RxObject
{
    private string _name = string.Empty;
    private bool _isBusy;
    
    public MainViewModel()
    {
        // Simple command
        SaveCommand = RxCommand.Create(Save);
        
        // Async command with cancellation
        LoadDataCommand = RxCommand.Create(LoadDataAsync);
        
        // Command with can execute logic
        var canDelete = this.WhenAnyValue(x => x.Name)
            .Select(name => !string.IsNullOrEmpty(name));
        DeleteCommand = RxCommand.Create(Delete, canDelete);
        
        // Command that returns a value
        ProcessCommand = RxCommand.Create<string, int>(ProcessData);
        
        // Subscribe to command results
        ProcessCommand.Subscribe(result => 
        {
            Console.WriteLine($"Processing completed with result: {result}");
        });
        
        // Handle errors
        ProcessCommand.ThrownExceptions.Subscribe(ex => 
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
        });
        
        // Track execution state
        LoadDataCommand.IsExecuting.Subscribe(isExecuting => 
        {
            IsBusy = isExecuting;
        });
    }
    
    public string Name
    {
        get => _name;
        set => RaiseAndSetIfChanged(ref _name, value);
    }
    
    public bool IsBusy
    {
        get => _isBusy;
        set => RaiseAndSetIfChanged(ref _isBusy, value);
    }
    
    public RxCommand<Unit, Unit> SaveCommand { get; }
    public RxCommand<Unit, Unit> LoadDataCommand { get; }
    public RxCommand<Unit, Unit> DeleteCommand { get; }
    public RxCommand<string, int> ProcessCommand { get; }
    
    private void Save()
    {
        // Save logic here
        Console.WriteLine($"Saving: {Name}");
    }
    
    private async Task LoadDataAsync()
    {
        // Simulate async operation
        await Task.Delay(3000);
        Name = "Loaded Data";
    }
    
    private void Delete()
    {
        Name = string.Empty;
        Console.WriteLine("Deleted!");
    }
    
    private int ProcessData(string input)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Input cannot be empty");
            
        return input.Length;
    }
}
```

### XAML Binding

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid>
        <StackPanel Margin="20">
            <TextBox Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}" 
                     Margin="0,0,0,10" />
            
            <Button Content="Save" 
                    Command="{Binding SaveCommand}" 
                    Margin="0,0,0,5" />
            
            <Button Content="Load Data" 
                    Command="{Binding LoadDataCommand}" 
                    Margin="0,0,0,5" />
            
            <Button Content="Delete" 
                    Command="{Binding DeleteCommand}" 
                    Margin="0,0,0,5" />
            
            <TextBox x:Name="InputTextBox" 
                     Margin="0,10,0,5" />
            <Button Content="Process" 
                    Command="{Binding ProcessCommand}" 
                    CommandParameter="{Binding Text, ElementName=InputTextBox}" 
                    Margin="0,0,0,5" />
            
            <ProgressBar IsIndeterminate="{Binding IsBusy}" 
                         Height="20" 
                         Margin="0,10,0,0" />
        </StackPanel>
    </Grid>
</Window>
```

## Advanced Scenarios

### Command with Complex Can Execute Logic

```csharp
public class OrderViewModel : RxObject
{
    private decimal _total;
    private bool _hasItems;
    private bool _isProcessing;
    
    public OrderViewModel()
    {
        // Complex can execute logic combining multiple conditions
        var canCheckout = Observable.CombineLatest(
            this.WhenAnyValue(x => x.Total),
            this.WhenAnyValue(x => x.HasItems),
            this.WhenAnyValue(x => x.IsProcessing),
            (total, hasItems, isProcessing) => 
                total > 0 && hasItems && !isProcessing);
        
        CheckoutCommand = RxCommand.Create(CheckoutAsync, canCheckout);
    }
    
    public decimal Total
    {
        get => _total;
        set => RaiseAndSetIfChanged(ref _total, value);
    }
    
    public bool HasItems
    {
        get => _hasItems;
        set => RaiseAndSetIfChanged(ref _hasItems, value);
    }
    
    public bool IsProcessing
    {
        get => _isProcessing;
        set => RaiseAndSetIfChanged(ref _isProcessing, value);
    }
    
    public RxCommand<Unit, Unit> CheckoutCommand { get; }
    
    private async Task CheckoutAsync()
    {
        IsProcessing = true;
        try
        {
            await Task.Delay(2000); // Simulate payment processing
            Console.WriteLine("Order processed successfully!");
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
```

### Command with Cancellation

```csharp
public class DataService
{
    public RxCommand<string, string> FetchDataCommand { get; }
    
    public DataService()
    {
        FetchDataCommand = RxCommand.Create<string, CancellationToken, string>(
            FetchDataAsync);
    }
    
    private async Task<string> FetchDataAsync(string url, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        
        try
        {
            var response = await httpClient.GetStringAsync(url, cancellationToken);
            return response;
        }
        catch (OperationCanceledException)
        {
            return "Operation was cancelled";
        }
    }
}
```

### Reactive Command Chaining

```csharp
public class WorkflowViewModel : RxObject
{
    public WorkflowViewModel()
    {
        Step1Command = RxCommand.Create(ExecuteStep1);
        Step2Command = RxCommand.Create(ExecuteStep2);
        Step3Command = RxCommand.Create(ExecuteStep3);
        
        // Chain commands together
        Step1Command
            .Where(result => result == "success")
            .InvokeCommand(Step2Command);
            
        Step2Command
            .Where(result => result == "success")
            .InvokeCommand(Step3Command);
    }
    
    public RxCommand<Unit, string> Step1Command { get; }
    public RxCommand<Unit, string> Step2Command { get; }
    public RxCommand<Unit, string> Step3Command { get; }
    
    private string ExecuteStep1()
    {
        Console.WriteLine("Executing Step 1");
        return "success";
    }
    
    private string ExecuteStep2()
    {
        Console.WriteLine("Executing Step 2");
        return "success";
    }
    
    private string ExecuteStep3()
    {
        Console.WriteLine("Executing Step 3");
        return "completed";
    }
}
```

## Key Features

### Execution State Tracking

```csharp
var command = RxCommand.Create(async () => 
{
    await Task.Delay(1000);
});

// Monitor execution state
command.IsExecuting.Subscribe(isExecuting => 
{
    Console.WriteLine($"Command is executing: {isExecuting}");
});
```

### Error Handling

```csharp
var riskyCommand = RxCommand.Create(() => 
{
    throw new InvalidOperationException("Something went wrong!");
});

// Handle errors reactively
riskyCommand.ThrownExceptions.Subscribe(ex => 
{
    Console.WriteLine($"Error: {ex.Message}");
});
```

### Using with Observable Sequences

```csharp
var numbers = Observable.Range(1, 10);
var processCommand = RxCommand.Create<int>(ProcessNumber);

// Use InvokeCommand to execute command for each item
numbers.InvokeCommand(processCommand);

private void ProcessNumber(int number)
{
    Console.WriteLine($"Processing: {number}");
}
```

## Best Practices

1. **Always subscribe to ThrownExceptions** for commands that might fail
2. **Use async commands** for I/O operations and long-running tasks
3. **Implement proper can execute logic** to improve user experience
4. **Dispose commands** when view models are disposed
5. **Use InvokeCommand** with observables for reactive command execution
6. **Leverage IsExecuting** for UI feedback during async operations

## Thread Safety

ReactiveMarbles.Command is designed to be thread-safe and handles marshalling to the UI thread automatically when used with appropriate schedulers.

```csharp
// Specify output scheduler for UI updates
var command = RxCommand.Create(
    DoWork, 
    outputScheduler: RxApp.MainThreadScheduler);
```

This library provides a powerful, reactive approach to command handling in C# applications, making it easier to build responsive and maintainable user interfaces.