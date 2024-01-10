# GameEvent 基于[Attribute]的Unity事件系统
GameEvent

一款基于C#属性[Attribute]的Unity事件系统

一款优雅的事件解决方案

### 基本用法演示
``` csharp
// 定义一个叫SomeEvt的事件
public struct SomeEvt : GameEvent.IGameEvent
{

}

// 订阅事件
[GameEvent.GameEvent]
private void OnSomeEvt(SomeEvt evt)
{
    Debug.Log("SomeEvt happened");
}

using GameEvent;
private void SomeMethod()
{
    // 发起 SomeEvt 事件
    var evt = new SomeEvt();
    evt.Invoke();
}
```
以上的例子中，三个部分，是可以在任意位置的

## 实现原理
使用Mono.Cecil，对程序集进行注入

由于是注入代码的关系，对类似HybridCLR（huatuo）等实现C# dll热更的解决方案，是原生支持的

## 安装
https://openupm.cn/packages/com.gm.gameevent/

## 配置
Unity中打开

ProjectSettings/GameEventSettings

### Assembly List
添加包含【事件定义】及【事件使用】的程序集名称

### Need Injected Log
勾选后，会在每次编译注入完成后，打印所有事件的使用日志

### 重新编译脚本 按钮
在修改了Assembly List后，点击，可及时重新编译脚本，

## 使用
### 初始化
```csharp
// 初始化API
GameEvent.GameEventDriver.Initialize(string assemblyName, bool throwOnError);

// 在任何事件发起前
// 在对应程序集加载后
// 对GameEventSettings.AssemblyList中填写的程序集，进行初始化
GameEvent.GameEventDriver.Initialize("Assembly-CSharp", true);
GameEvent.GameEventDriver.Initialize("OtherAssembly", true);
...
```
### 定义事件
事件分为两种，一种是同步事件，一种是异步事件

继承IGameEvent或IGameTask，即可定义事件

定义事件的类型，可以是struct，可以是class，随意选择

1，同步事件
```csharp
public struct SyncEvt : GameEvent.IGameEvent
{
    // 事件信息可自定义（可以没有）
    // 事件参数1
    public int param1;
    public GameObject param2;
    ...
}
```
2，异步事件
```csharp
public struct AsyncEvt : GameEvent.IGameTask
{
    // 事件信息部分同上
}
```

### 订阅事件
[GameEvent.GameEvent]属性

参数 (bool CallOnlyIfMonoEnable = false) 意为，当该函数的所属对象是MonoBehaviour时，会额外判断，MonoBehaviour是否Enable，Enable=true时才会被触发事件

注：订阅事件，是以对象为单位的，即某个对象的订阅和取消订阅，会将对象上的所有Game Event，统一订阅或取消

1，同步事件

在任何返回值为void的函数上，添加[GameEvent]属性

且参数是对应的事件类型本身，即为订阅事件

非静态函数，必须是属于class对象的，struct中的非静态函数，不能订阅事件

```csharp
public class FooObject
{
    [GameEvent]
    private void OnSyncEvt(SyncEvt evt)
    {
        Debug.Log("OnSyncEvt");
    }
    
    [GameEvent]
    private static void OnSyncEvt_Static(SyncEvt evt)
    {
        Debug.Log("OnSyncEvt_Static");
    }
}
```
2，异步事件

在任何返回值为Task(或Task<>)的函数上，添加[GameEvent]属性

且参数是对应的事件类型本身，即为订阅事件

```csharp
public class FooObject
{
    [GameEvent]
    private async Task OnAsyncEvt(AsyncEvt evt)
    {
        Debug.Log("OnAsyncEvt");
    }
    
    [GameEvent]
    private static Task OnAsyncEvt_Static(AsyncEvt evt)
    {
        Debug.Log("OnAsyncEvt_Static");
    }
}
```
以上的事件订阅均是自动发生的

3，手动订阅

如发生手动取消订阅后，又想要重新订阅的情况，提供了手动订阅的API
``` csharp
// 传入 需要订阅的对象
GameEvent.GameEventDriver.Register(object target);
```


### 取消订阅

如订阅者为MonoBehaviour，则在销毁后，自动取消订阅

如订阅者为非MonoBehaviour的对象，则需要手动取消订阅

手动取消订阅API
``` csharp
// 传入 需要取消订阅的对象
GameEvent.GameEventDriver.Unregister(object target);
```

### 发布事件

1，同步事件
```csharp
var evt = new SyncEvt();
// 需要using GameEvent;
evt.Invoke();
```
2，异步事件
```csharp
var evt = new AsyncEvt();
// 需要using GameEvent;
await evt.InvokeTask();
```

### 打包相关

如果使用了类似HybridCLR（huatuo）等实现C# dll热更的解决方案，
则需要自行在打包后，对程序集进行注入

API
```csharp
GameEvent.GlobalEventInjecter.InjectEvent(string dir, params string[] dllFileArray)
// dir 为程序集目录，如 "./Library/ScriptAssemblies"
// dllFileArray 为 GameEventSettings.AssemblyList 中填写的程序集名称
```

### 最后一些小问题

当一个对象存在继承关系时

如子类和父类均订阅了同一个事件，则只有子类中的事件会响应

### 最后

觉得有趣的点个Star~

谢谢~