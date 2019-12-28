using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RpiHost.Models
{
    public class RequestWithId<T>
    {
        public T Id { get; set; }
    }
}
