# Code Generated Events

The ModernUO code generated events is an alternative to using `delegate` events in C#.

## How to Install

Add `ModernUO.CodeGeneratedEvents` as an analyzer project reference:

```xml
    <ItemGroup>
        <PackageReference Include="ModernUO.CodeGeneratedEvents" Version="1.0.0">
            <SetTargetFramework>TargetFramework=netstandard2.0</SetTargetFramework>
            <OutputItemType>Analyzer</OutputItemType>
            <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
            <PrivateAssets>all</PrivateAssets>
        </PackageReference>
    </ItemGroup>
```

## Usage

In the file that needs to raise an event:
```cs
public class AchievementSystem
{
    [OnEvent(BaseCreature.CreatureKilledEvent)]
    public static void OnCreatureKilled(BaseCreature creature, Mobile killer)
    {
        // Achievement logic here
    }
}
```

In the file invoking the event:
```cs
public partial class BaseCreature
{
    public const string CreatureKilledEvent = "CreatureKilled";

    [GeneratedEvent(CreatureKilledEvent)]
    public static partial void InvokeCreatureKilledEvent(BaseCreature creature, Mobile killer);
}
```

> [!Note]
> Note the use of `partial` for the class that uses `GeneratedEvent`.
> This is required for the source generator to build the code.

## Notes

- Event names must be unique.
- Event names must be constant strings.
- `OnEvent` and `GeneratedEvent` methods must have the same signature and return void;
- `OnEvent` methods may have multiple OnEvent attributes.
