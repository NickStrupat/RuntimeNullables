# Runtime Nullables

[![Chat on Discord](https://img.shields.io/discord/906246067773923490)](https://discord.gg/EkQhJFsBu6)
[![View nuget packages](https://img.shields.io/nuget/v/RuntimeNullables.Fody.svg)](https://www.nuget.org/packages/RuntimeNullables.Fody/)
[![Downloads](https://img.shields.io/nuget/dt/RuntimeNullables.Fody)](https://www.nuget.org/packages/RuntimeNullables.Fody/)
[![Build and Test](https://github.com/Singulink/RuntimeNullables/workflows/build%20and%20test/badge.svg)](https://github.com/Singulink/RuntimeNullables/actions?query=workflow%3A%22build+and+test%22)

**Runtime Nullables** automatically adds null checks to method/property entry and exit points based on the standard nullable annotations and attributes available in C# 8+. It is capable of checking input parameters as well as outputs (i.e. return values and `out`/`ref` parameters) and supports comprehensive checks on the full range of special method types including asynchronous `Task<T>` methods, `Task<T>` methods that synchronously return completed tasks (i.e. using `Task.FromResult<T>`), `IEnumerable<T>`/`IEnumerator<T>` iterators as well as asynchronous `IAsyncEnumerable<T>`/`IAsyncEnumerator<T>` iterators. Custom throw helpers can be defined to fully customize the exceptions thrown to your liking.

### About Singulink

We are a small team of engineers and designers dedicated to building beautiful, functional and well-engineered software solutions. We offer very competitive rates as well as fixed-price contracts and welcome inquiries to discuss any custom development / project support needs you may have.

Visit https://github.com/Singulink to see our full list of publicly available libraries and other open-source projects.

## Installation

The package is available on NuGet - simply install the `RuntimeNullables.Fody` package into your project and null checks will be automatically injected when your project builds! It is also a good idea to add the latest `Fody` package directly to your project to ensure you are using an up-to-date version to avoid issues.

## Runtime Nullables v.s. NullGuard.Fody

After adding nullable annotation support to NullGuard on top of its legacy functionality, it was apparent that it become rather unweildy. This made expanding features and optimizing behavior for nullable annotations difficult in addition to inheriting an overly complex attribute model. **Runtime Nullables** was written from the ground up with only nullable annotations in mind to address these issues. Some notable improvements include:

- More efficient weaving algorithm for faster build times
- Supports checking `ValueTask<T>` results, synchronously returned complete `Task<T>` results, `IEnumerable<T>`/`IEnumerator<T>` iterator values and asynchronous `IAsyncEnumerable<T>`/`IAsyncEnumerator<T>` iterator values
- Uses throw helpers instead of throwing directly (better performance / smaller IL code) and lets you define your own custom throw helpers
- Much simpler attribute model that uses a single `[NullChecks(bool)]` attribute to control null check injection
- Designed specifically for NRTs so it is much easier to add advanced functionality such as full validation of conditional attributes like `[MaybeNullWhen]` (planned for a future release)
- Outputs warnings for conflicting annotations that cause null checks to be skipped, i.e. if `[AllowNull, DisallowNull]` is applied to a parameter
- Uses `NullReferenceException` instead of `InvalidOperationException` when an output check fails since the latter is often thrown/caught in normal circumstances which can cause null contract violations to go unnoticed in unit tests or exception handling code
- Does not forcibly add `.initlocals` to methods (which overrides `[SkipLocalsInit]` behavior)
- Numerous bug fixes and reliability improvements

## Configuration

By default, null checks are added based on the build configuration as follows:
- `Release`: input parameter values on publicly exposed methods/properties only
- `Debug`: input and output (`ref`/`out`) parameter values and return values on all methods/properties

The behavior can be configured in one of two ways: inside your `.csproj` file or using a `FodyWeavers.xml` configuration file in the root of your project. There are two configuration settings:

- `CheckOutputs`: If set to `true` then outputs (i.e. return values, ref/out parameters) will be null checked, otherwise these checks are omitted.
- `CheckNonPublic`: If set to `true` then all methods/properties are checked, otherwise only those that are potentially publicly exposed are checked.

If configuring via the `.csproj` file then you have to add a `<WeaverConfiguration>` property like so:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <WeaverConfiguration>
      <Weavers>
        <RuntimeNullables CheckNonPublic="true" CheckOutputs="true" />
      </Weavers>
    </WeaverConfiguration>
  </PropertyGroup>
</Project>
```

If you would like to configure settings with a `FodyWeavers.xml` file in the project then simply build the project after adding the `RuntimeNullables.Fody` package and it will automatically generate an xml configuration file for you which you can then edit. Alternatively, you can add the file manually with the following contents:

```xml
<Weavers xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:noNamespaceSchemaLocation="FodyWeavers.xsd">
  <RuntimeNullables CheckNonPublic="true" CheckOutputs="true" />
</Weavers>
```

If you omit the `CheckNonPublic` or `CheckOutputs` setting then it will fallback to the default behavior based on the build configuration as explained above.

## Fine-Tuning Behavior

Injection of null checks can be fine-tuned in your code using the `[NullChecks(bool)]` attribute which can be applied at the assembly, class, method/property or return value level. For example, if you apply `[NullChecks(false)]` to a class it will disable null checks on every member in that class unless it has `[NullChecks(true)]` applied to it. It is important to note that if the `CheckOutputs` or `CheckNonPublic` setting is `false` then that will override any `[NullChecks(true)]` attribute applied to an output or non-public member. In other words, if the setting is set to `false` then those particular checks are never emitted, regardless of the presence of a `[NullChecks(true)]` attribute.

Additionally, custom throw helpers can be defined to customize the exceptions that are thrown. In order to do that, you simply add an internal `ThrowHelpers` class into your project in the `RuntimeNullables` namespace and match the signature of the throw helper you want to override and it will be used instead of the defaults. The default throw helpers are as follows:

```c#
namespace RuntimeNullables
{
    internal static class ThrowHelpers
    {
        internal static void ThrowArgumentNull(string paramName)
        {
            throw new ArgumentNullException(paramName);
        }

        internal static void ThrowOutputNull(string message)
        {
            throw new NullReferenceException(message);
        }

        internal static Exception GetAsyncResultNullException(string message)
        {
            return new NullReferenceException(message);
        }
    }
}
```

If an output check or async result check fails then the `message` parameter contains a message specifying exactly what caused the check to fail, i.e. "Enumerator result nullability contract was broken."

## Examples

```C#
using RuntimeNullables;

// Return value is not null checked since it is nullable,
// but method throws ArgumentNullException if messageService is null
public string? GetMessage(MessageService messageService);

// The 'value' parameter is null checked on method entry as well as when the method exits since it is a ref
public void UpdateMessage(ref string value);

// Now the 'value' parameter is only null checked on method entry due to the [MaybeNull] annotation
public void UpdateMessage([MaybeNull] ref string value);

// Each item returned by the enumeration is null checked
public IEnumerable<string> GetMessages()
{
    string? nullValue = null;
    yield return "some value";
    yield return "some other value";
    yield return nullValue!; // NullReferenceException is thrown
}

public Task<T> GetValueAsync<T>()
{
    return Task.FromResult<T>(default!); // NullReferenceException if T is a reference type
}

[return: NullChecks(false)]
public Task<T> GetValueAsync<T>()
{
    return Task.FromResult<T>(default!); // This is okay now since return value null checks are disabled
}

// Result of the task is null checked
public async Task<string> GetMessageAsync()
{
    return await MessageService.GetMessageAsync();
}

// All results are null checked
public async IAsyncEnumerable<string> GetValuesAsync()
{
    await Task.Delay(100);
    yield return "Some value";
    await Task.Delay(100);
    yield return null!; // NullReferenceException is thrown
}
```
