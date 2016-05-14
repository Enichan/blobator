using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blobator.Json {
    public class LowercaseContractResolver : DefaultContractResolver {
        protected override string ResolvePropertyName(string propertyName) {
            return propertyName.Substring(0, 1).ToLowerInvariant() + propertyName.Substring(1);
        }
    }
}
