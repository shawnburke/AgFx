using System;

namespace AgFx.Controls.Authorization {
    
    public abstract class LoginDataLoader<T> : IDataLoader<T> where T : LoginLoadContext {

        /// <summary>
        /// Will be called when LoginLoadContext.CanAttemptLogin is true
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected abstract LoadRequest BuildLoginRequest(T context);


        public LoadRequest GetLoadRequest(T loadContext, Type objectType) {

            if (!loadContext.CanAttemptLogin) {
                return null;
            }
            return BuildLoginRequest(loadContext);
        }

        public abstract object Deserialize(T loadContext, Type objectType, System.IO.Stream stream);
    }
}
