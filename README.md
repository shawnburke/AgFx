# AgFx Windows Phone App and Data Caching Framework
### Build great data-connected Windows Phone 7 applications.

Welcome to AgFx, a framework for building Windows Phone 7 applications. This framework exists because many apps for Windows Phone 7 run into the same types of problems around managing data, keeping work off of the UI thread, and optimizing network usage. It's goal is to make all of this easy so you can focus on writing your application.

To get started with AgFx, check out [this tutorial](http://agfx.codeplex.com/wikipage?title=Tutorial&referringTitle=Home) which not only walks through the basic process of building an AgFx-based application, but also gives an overview of what the framework can do.

AgFx currently supports:

* Windows Phone 7
* Windows Phone 7.5 (Mango)

### Project Status

AgFx is in the process of being moved to GitHub from it's original home on [CodePlex](http://agfx.codeplex.com)

### NuGet
AgFx is also available via NuGet [here](http://nuget.org/List/Packages/AgFx).

### Features

* Automatic caching and retrieval of data from network requests 
* Automatically handles determines if cache is valid or if a new valid data needs to be fetched 
* Easy framework for building databound view model objects based on cached data 
* Instance tracking ensures that all parts of your app are referencing the same instance - an update in one place will update data in other places 
* Flexible framework for defining how objects are cached, how long the cache is valid, and how to handle invalid cache items, etc. 
* Simple framework for app-wide broadcast messages 
* Many helper classes for implementing common pattern in Windows Phone 7 apps. 
* Debugging features to allow you to see what AgFx is doing and see reports for timings of how the network fetch and deserilization parts of your app are performing.

### Usage Overview
In brief, AgFx allows you to describe the two things that it can't automatically figure out, and then it does everything else.

There are three things that each data-connected application has to describe:

* How to fetch it's data. This is usually an URL, but could be other things. 
* How to deserialize that data, once fetched, into a model or view model object 
* How long the data is valid for, and what to do when it expires. In other words, after what point should new data be fetched, and should invalid cached data be

With that information, which you define for each of your objects, AgFx can handle the rest.

For more information about how AgFx handles data, check out [How AgFx Works](http://agfx.codeplex.com/wikipage?title=How%20AgFx%20Works&referringTitle=Home).