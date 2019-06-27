using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeExamples
{
    public class ObjectSequence
    {
        public string ObjectId { get; set; }

        public int Sequence { get; set; }
    }

    public class SearchValueObject
    {
        public string SearchValue { get; set; }

        public int Sequence { get; set; }
    }

    public class SearchValuesObject
    {
        public IEnumerable<SearchValueObject> SearchValues { get; set; }
        
    }
}
