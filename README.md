# Sharpin
Simple WIP Mixin Framework for C# using Mono.Cecil

## Usage

### Inject

```csharp
[Mixin(typeof(RudeBear))]
public abstract class MixinRudeBear : RudeBear {
    [Inject(method = "System.Void RudeBear::CommonDeath(System.Int32)", at = "HEAD", cancellable = true)]
    public void CommonDeath(int specialText, CallbackInfo info) {
        if(RudeAPIHooks.OnDeath(this)) {
            Debug.Log("Not today!");
            info.Cancel();
        }
    }
}
```

will (simplified) result in

```csharp
public class RudeBear {
    // [...]
    public void CommonDeath(int specialText) {
        if(RudeAPIHooks.OnDeath(this)) {
            Debug.Log("Not today!");
            return;
        }
        // [... the rest of CommonDeath ...]
    }
    // [...]
}
```

### Overwrite

```csharp
[Mixin(typeof(SpearKill))]
public abstract class MixinSpearKill : SpearKill {
    [Overwrite]
    public void OnTriggerEnter2D(UnityEngine.Collider2D col) {
        UnityEngine.Debug.Log("Spears were never meant to be used for killing.");
    }
}
```

### CaptureLocal & StoreLocal

Pretend we have `SomeCharacter` with an `OnDamage` function that takes a base value, and inside it does calculations to get a modified value based on the character's armor stats or something.

These are the locals (with name, pretend it's a Debug build):
```
.locals init (
    [0] int32 armorStat,
    [1] int32 modifiedDamage
)
```

Sadly locals lose their names on Release builds, so we have to access them by index.
The following will grab the `modifiedDamage` right before it's applied via `SomeCharacter::ReduceHealth`

[Inject(method = "System.Void SomeCharacter::OnDamage(System.Int32)", at = "IL_0017: call System.Void SomeCharacter::ReduceHealth(System.Int32)")]
public void OnDamage(int baseDamage, [CaptureLocal(1, typeof(int))] int modifiedDamage, [StoreLocal(1, typeof(int))] out int outModifiedDamage) {
    if(modifiedDamage == 1336) { // Ugh, so close. Let's just sneakily add one to make it happen.
        outModifiedDamage = modifiedDamage + 1;
    }
}

### Injection Points

There are two inbuilt injection points for the `at` parameter: `HEAD` and `RETURN`.

- `HEAD` will always be right before the first statement of the target method
- `RETURN` will be before any return statement in the target method - this can be multiple, so set the expectedInjections attribute accordingly

Apart from these, specific instructions can be named in a similar manner that you would see them in ILSpy, for example:

`IL_0017: call System.Void SomeCharacter::ReduceHealth(System.Int32)`

> Note that injection points will always be shifted backwards to a state of empty stack. This means that if we have bytecode that looks like

```
IL_0009: ldloc.0
IL_000a: ret
```

> and we specify `RETURN` or `IL_000a: ret` as injection point, the actual injection point will end up right before `IL_0009: ldloc.0`.