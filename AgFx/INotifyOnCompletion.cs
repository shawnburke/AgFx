// 
// This is a one-off customization I depend on in my app right now w.r.t. AgFx.
// 

using System;

namespace AgFx
{
    public interface INotifyOnCompletion
    {
        void OnCompletion(Exception ex);
    }
}
