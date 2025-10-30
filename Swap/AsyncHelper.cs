// --- Await helpers (covers Task, Task<T>, ValueTask, ValueTask<T>, or plain object) ---
    private static async Task<object?> AwaitTaskLikeOrReturn(object? obj)
    {
        if (obj is null) return null;

        // Task / Task<T>
        if (obj is Task t)
        {
            await t.ConfigureAwait(false);
            var tt = t.GetType();
            if (tt.IsGenericType)
                return tt.GetProperty("Result")!.GetValue(t);
            return null;
        }

        // ValueTask / ValueTask<T> via reflection: call AsTask()
        var type = obj.GetType();
        if (type.FullName!.StartsWith("System.Threading.Tasks.ValueTask"))
        {
            var asTask = type.GetMethod("AsTask", Type.EmptyTypes);
            if (asTask is not null)
            {
                var taskObj = (Task)asTask.Invoke(obj, null)!;
                await taskObj.ConfigureAwait(false);
                var taskType = taskObj.GetType();
                if (taskType.IsGenericType)
                    return taskType.GetProperty("Result")!.GetValue(taskObj);
                return null;
            }
        }

        // Synchronous return (already the payload)
        return obj;
    }
