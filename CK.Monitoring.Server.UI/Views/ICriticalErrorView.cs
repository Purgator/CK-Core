﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Monitoring.Server.UI
{
    public interface ICriticalErrorView
    {
        void AddCriticalError( string error );
    }
}
