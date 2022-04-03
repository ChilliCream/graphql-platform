using System;
using System.Buffers;
using System.Collections.Concurrent;
using Microsoft.Extensions.ObjectPool;

namespace GreenDonut;

/// <summary>
/// Owner of <see cref="TaskCache"/> that is responsible for returning the rented
/// <see cref="TaskCache"/> appropriately to the <see cref="ObjectPool{TaskCache}"/>.
/// </summary>
public sealed class TaskCacheOwner : IDisposable
{
   
}