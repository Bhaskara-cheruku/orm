using System.Xml;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Custom_ORM.Data
{
    public class SchemaSnapshot
    {
        public Dictionary<string, TableSchema> Tables { get; set; }

        public SchemaSnapshot()
        {
            Tables = new Dictionary<string, TableSchema>();
        }

        public class TableSchema
        {
            public List<string> Columns { get; set; }
            public List<string> ForeignKeys { get; set; }

            public TableSchema()
            {
                Columns = new List<string>();
                ForeignKeys = new List<string>();
            }
        }

        // Serialize schema snapshot to JSON string
        public static string Serialize(SchemaSnapshot snapshot)
        {
            return JsonConvert.SerializeObject(snapshot, Newtonsoft.Json.Formatting.Indented);
        }

        // Deserialize from JSON string
        public static SchemaSnapshot Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<SchemaSnapshot>(json);
        }
    }
}
