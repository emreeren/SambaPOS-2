using System;
using System.Collections;

namespace Samba.Infrastructure
{
    public class MessagingClientObject : MarshalByRefObject, IObserver
    {
        private readonly ArrayList _newData = ArrayList.Synchronized(new ArrayList());

        public int GetData(out string[] arrData)
        {
            
            if (_newData.Count > 0)
            {
                arrData = new String[_newData.Count];
                try
                {
                    _newData.CopyTo(0, arrData, 0, arrData.Length);
                    _newData.RemoveRange(0, arrData.Length);
                }
                catch
                {
                    
                }
               
                return arrData.Length;
            }
            arrData = null;
            return 0;
        }

        public  bool Update(ISubject sender, string data, short objState)
        {
            _newData.Add(data);
            return true;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
