using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Profiling;


namespace Glowwave
{
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class NoSerialize : System.Attribute
    {

    }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class SerializeType : System.Attribute
    {

    }

    public class Json
    {
        public static long firebaseTimestampLongPlaceholder {
            get {
                return long.MinValue;
            }
        }

        public static Dictionary<string, string> firebaseTimestampPlaceholder {
            get {
                Dictionary<string, string> result = new Dictionary<string, string>();
                result[".sv"] = "timestamp";
                return result;
            }
        }

        public class CustomSerializer
        {
            struct Entry
            {
                public System.Type type;
                public System.Reflection.MethodInfo method;
            }

            public bool HandleToJson(object obj, out object result)
            {
                System.Type type = obj.GetType();

                foreach(Entry entry in _tojson) {
                    if(entry.type == type) {
                        object[] args = new object[] { obj };
                        result = entry.method.Invoke(this, args);
                        return true;
                    }
                }

                result = null;

                return false;
            }

            public bool HandleFromJson(object obj, System.Type type, out object result)
            {
                foreach(Entry entry in _fromjson) {
                    if(entry.type == type) {
                        object[] args = new object[] { obj };
                        result = entry.method.Invoke(this, args);
                        return true;
                    }
                }
                result = null;
                return false;
            }

            List<Entry> _tojson = new List<Entry>();
            List<Entry> _fromjson = new List<Entry>();

            public CustomSerializer()
            {
                Debug.Log("REGISTER_JSON CustomSerializer()");
                System.Type t = this.GetType();
                System.Reflection.MethodInfo[] methods = t.GetMethods();
                foreach(var method in methods) {
                    Debug.Log("REGISTER_JSON: Method = " + method.Name);
                    if(method.Name == "ToJson") {
                        System.Reflection.ParameterInfo[] parameters = method.GetParameters();
                        if(parameters.Length == 1) {
                            Entry entry = new Entry() {
                                type = parameters[0].ParameterType,
                                method = method,
                            };

                            Debug.Log("REGISTER_JSON: ToJson: " + method.Name);
                            _tojson.Add(entry);
                        }
                    } else if(method.Name.Length >= 8 && method.Name.Substring(0,8) == "FromJson") {
                        Entry entry = new Entry() {
                            type = method.ReturnType,
                            method = method,
                        };

                        Debug.Log("REGISTER_JSON: FromJson: " + method.Name);

                        _fromjson.Add(entry);
                    }
                }
            }
        }

        public class GameSerializer : CustomSerializer
        {
            public object ToJson(Loc loc)
            {
                List<int> result = new List<int>();
                if(loc.valid == false) {
                    return result;
                }

                result.Add(loc.x);
                result.Add(loc.y);
                if(loc.depth != 1) {
                    result.Add(loc.depth);
                }
                return result;
            }

            public Loc FromJson(object obj)
            {
                List<object> items = obj as List<object>;
                if(items != null) {
                    if(items.Count == 2) {
                        return new Loc(System.Convert.ToInt32(items[0]), System.Convert.ToInt32(items[1]));
                    } else if(items.Count == 3) {
                        return new Loc(System.Convert.ToInt32(items[0]), System.Convert.ToInt32(items[1]), System.Convert.ToInt32(items[2]));
                    }
                }

                return Loc.invalid;
            }

            public object ToJson(HashSet<Loc> locs)
            {
                List<object> result = new List<object>();
                foreach(Loc loc in locs) {
                    //use run-length encoding. Discard anything with a loc to the left
                    //since it is part of a run.

                    Loc leftLoc = new Loc(loc.x-1, loc.y, loc.depth);
                    if(locs.Contains(leftLoc)) {
                        continue;
                    }

                    //If there is a loc to the right, we have a run.
                    int runLength = 0;
                    while(true) {
                        Loc nextLoc = new Loc(loc.x+runLength+1, loc.y, loc.depth);
                        if(locs.Contains(nextLoc) == false) {
                            break;
                        }
                        ++runLength;
                    }

                    List<int> encoded = new List<int>();
                    encoded.Add(loc.x);
                    encoded.Add(loc.y);

                    if(runLength > 0 || loc.depth != 1) {
                        encoded.Add(loc.depth);
                    }

                    if(runLength > 0) {
                        encoded.Add(runLength);
                    }

                    result.Add(encoded);
                }

                return result;
            }

            public HashSet<Loc> FromJsonSet(object obj)
            {
                HashSet<Loc> result = new HashSet<Loc>();
                List<object> items = obj as List<object>;

                foreach(object item in items) {
                    List<object> nums = item as List<object>;
                    if(nums != null) {
                        int runLength = 0;
                        if(nums.Count == 4) {
                            runLength = System.Convert.ToInt32(nums[3]);
                            List<object> newNums = new List<object>(nums);
                            newNums.RemoveAt(3);
                            nums = newNums;
                        }

                        Loc loc = FromJson(nums);
                        for(int i = 0; i <= runLength; ++i) {
                            result.Add(loc);
                            loc = new Loc(loc.x+1, loc.y, loc.depth);
                        }
                    }
                }

                return result;
            }
        }

        static ProfilerMarker s_profileToJson = new ProfilerMarker("Serialize.ToJson");
        static ProfilerMarker s_profileClass = new ProfilerMarker("Serialize.Class");
        static ProfilerMarker s_profileDict = new ProfilerMarker("Serialize.Dict");
        static ProfilerMarker s_profileList = new ProfilerMarker("Serialize.List");


        static ProfilerMarker s_profileEscapeString = new ProfilerMarker("Serialize.EscapeString");
        static ProfilerMarker s_profileCustomJsonEncode = new ProfilerMarker("Serialize.CustomJsonEncode");
        static ProfilerMarker s_profileCustomHandle = new ProfilerMarker("Serialize.CustomHandle");



        public static string EscapeString(string s)
        {
            using(s_profileEscapeString.Auto()) {
                return BestHTTP.JSON.Json.Encode(s);
            }
        }

        public static string ToJson(object obj, CustomSerializer customSerializer=null)
        {
            using(s_profileToJson.Auto()) {
                if(obj == null) {
                    return "null";
                }

                if(customSerializer != null) {
                    object customOutput = null;
                    s_profileCustomHandle.Begin();
                    bool resultCustom = customSerializer.HandleToJson(obj, out customOutput);
                    s_profileCustomHandle.End();
                    if(resultCustom) {
                        using(s_profileCustomJsonEncode.Auto()) {
                            return BestHTTP.JSON.Json.Encode(customOutput);
                        }
                    }
                }

                System.Type t = obj.GetType();
                if(obj is int || obj is float || obj is double) {
                    return obj.ToString();
                } else if(obj is long) {
                    if((long)obj == Glowwave.Json.firebaseTimestampLongPlaceholder) {
                        return ToJson(Glowwave.Json.firebaseTimestampPlaceholder);
                    }

                    return obj.ToString();
                } else if(obj is bool) {
                    bool b = (bool)obj;
                    return b ? "true" : "false";
                } else if(obj is string) {
                    return EscapeString(obj as string);
                } else if(t.IsEnum) {
                    return ((int)obj).ToString();
                } else if(typeof(IEnumerable).IsAssignableFrom(t)) {

                    IEnumerable e = (IEnumerable)obj;
                    bool first = true;

                    //dictionaries keyed by strings are serialized as maps.
                    if(typeof(IDictionary).IsAssignableFrom(t) && t.GetGenericArguments()[0] == typeof(string)) {
                        using(s_profileDict.Auto()) {
                            string res = "{";
                            foreach(object item in e) {
                                System.Type entryType = item.GetType();

                                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public;

                                object key = entryType.GetField("key", flags).GetValue(item);
                                object val = entryType.GetField("value", flags).GetValue(item);

                                if(!first) {
                                    res += ",";
                                }

                                first = false;

                                res += BestHTTP.JSON.Json.Encode(key) + ": " + ToJson(val, customSerializer);
                            }

                            res += "}";
                            return res;
                        }
                    }

                    using(s_profileList.Auto()) {
                        string result = "[";
                        foreach(object item in e) {
                            if(!first) {
                                result += ",";
                            }
                            first = false;

                            result += ToJson(item, customSerializer);
                        }
                        result += "]";
                        return result;
                    }
                } else if(obj is GWScriptableObject) {
                    GWScriptableObject scriptObj = obj as GWScriptableObject;
                    return EscapeString(scriptObj.guidNameSet + ":" + scriptObj.guid);
                } else if(t.IsClass || t.IsValueType) {
                    using(s_profileClass.Auto()) {
                        string result = "{";

                        bool first = true;

                        if(t.IsClass) {
                            bool serializeClass = false;
                            foreach(System.Reflection.CustomAttributeData attr in t.CustomAttributes) {
                                if(attr.AttributeType == typeof(SerializeType)) {
                                    serializeClass = true;
                                    break;
                                }
                            }

                            if(serializeClass) {
                                result += "\"@class\": \"" + t.FullName + "\""; //, " + t.Assembly.GetName().Name + "\"";
                                first = false;
                            }
                        }

                        System.Reflection.FieldInfo[] fields = t.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public);
                        foreach(var field in fields) {

                            bool noserialize = false;

                            foreach(System.Reflection.CustomAttributeData attr in field.CustomAttributes) {
                                if(attr.AttributeType == typeof(NoSerialize)) {
                                    noserialize = true;
                                    break;
                                }
                            }

                            if(noserialize) {
                                continue;
                            }

                            if(!first) {
                                result += ",";
                            }
                            first = false;

                            object val = field.GetValue(obj);
                            result += "\"" + field.Name + "\": " + ToJson(val, customSerializer);
                        }

                        result += "}";
                        return result;
                    }
                }

                return "<ERROR>";
            }
        }

        public static object DecodeJson(object obj, System.Type t, CustomSerializer customSerializer)
        {
            if(obj == null) {
                return null;
            }

            if(t.IsPrimitive) {
                if(t == typeof(int)) {
                    return System.Convert.ToInt32(obj);
                } else if(t == typeof(long)) {
                    return System.Convert.ToInt64(obj);
                } else if(t == typeof(bool)) {
                    return obj;
                } else if(t == typeof(float)) {
                    return System.Convert.ToSingle(obj);
                } else if(t == typeof(double)) {
                    return (double)obj;
                } else {
                    Debug.LogError("Cannot parse primitive type: " + t.ToString());
                    return null;
                }
            } else if(t.IsEnum) {
                return System.Enum.ToObject(t, System.Convert.ToInt32(obj));
            } else if(t.IsArray) {
                IList list = obj as IList;
                if(list == null) {
                    Debug.LogError("Could not convert " + obj.ToString() + " to array");
                    return null;
                }

                System.Type elementType = t.GetElementType();

                System.Array array = System.Array.CreateInstance(elementType, list.Count);

                for(int i = 0; i != list.Count; ++i) {
                    array.SetValue(DecodeJson(list[i], elementType, customSerializer), i);
                }

                return array;
            } else if(t.IsClass || t.IsValueType) {
                if(t == typeof(string)) {
                    return obj;
                } else if(typeof(IList).IsAssignableFrom(t)) {
                    IList list = obj as IList;
                    if(list == null) {
                        Debug.LogError("Could not convert " + obj.ToString() + " to list");
                        return null;
                    }

                    System.Type[] args = t.GetGenericArguments();
                    if(args == null || args.Length < 1) {
                        Debug.Log("List doesn't have generic arguments in serialization: " + t.ToString());
                        return null;
                    }

                    System.Type elementType = args[0];

                    IList result = System.Activator.CreateInstance(t) as IList;

                    for(int i = 0; i != list.Count; ++i) {
                        result.Add(DecodeJson(list[i], elementType, customSerializer));
                    }

                    return result;
                } else if(typeof(IDictionary).IsAssignableFrom(t)) {

                    IDictionary result = System.Activator.CreateInstance(t) as IDictionary;

                    System.Type[] args = t.GetGenericArguments();
                    if(args == null || args.Length < 1) {
                        Debug.Log("Dictionary doesn't have generic arguments in serialization: " + t.ToString());
                        return null;
                    }

                    System.Type keyType = args[0];
                    System.Type valueType = args[1];

                    Dictionary<string, object> dict = obj as Dictionary<string, object>;
                    if(dict != null) {
                        foreach(KeyValuePair<string, object> p in dict) {
                            result[p.Key] = DecodeJson(p.Value, valueType, customSerializer);
                        }

                        return result;
                    }

                    IList list = obj as IList;
                    if(list == null) {
                        Debug.LogError("Could not convert " + obj.ToString() + " to list");
                        return null;
                    }


                    for(int i = 0; i != list.Count; ++i) {
                        Dictionary<string, object> valueDict = list[i] as Dictionary<string, object>;
                        if(valueDict == null) {
                            Debug.Log("Invalid KeyValuePair value in dictionary list: " + list[i].ToString());
                            return null;
                        }

                        object key = DecodeJson(valueDict["key"], keyType, customSerializer);
                        object val = DecodeJson(valueDict["value"], valueType, customSerializer);

                        result[key] = val;
                    }

                    return result;

                } else if(typeof(GWScriptableObject).IsAssignableFrom(t)) {
                    string str = obj as string;
                    if(GWScriptableObject.allObjects.ContainsKey(str) == false) {
                        Debug.LogError("Could not lookup GWScriptableObject by guid: '" + str + "'");
                        return null;
                    }

                    return GWScriptableObject.allObjects[str];
                } else {
                    return ClassFromJson(obj, t, customSerializer);
                }
            }

            return null;
        }

        public static T FromJson<T>(string s, CustomSerializer customSerializer=null)
        {
            return FromObject<T>(BestHTTP.JSON.Json.Decode(s), customSerializer);
        }

        public static T FromObject<T>(object obj, CustomSerializer customSerializer)
        {
            return (T)ClassFromJson(obj, typeof(T), customSerializer);
        }

        static object ClassFromJson(object obj, System.Type typeHint, CustomSerializer customSerializer)
        {
            if(obj == null) {
                return null;
            }

            object customResult = null;
            if(customSerializer != null && customSerializer.HandleFromJson(obj, typeHint, out customResult)) {
                return customResult;
            }

            Dictionary<string, object> dict = obj as Dictionary<string, object>;
            if(dict == null) {
                Debug.LogError("Could not parse class " + typeHint.ToString() + " Found a " + obj.ToString() + " when dict expected");
                return null;
            }

            System.Type t = null;

            object className = null;
            if(dict.TryGetValue("@class", out className)) {
                t = System.Type.GetType((string)className);
                if(t == null) {
                    Debug.LogError("Could not load class: " + (string)className);
                }
            }

            if(t == null) {
                t = typeHint;
            }

            if(t == null) {
                return null;
            }

            object result = System.Activator.CreateInstance(t);
            System.Reflection.FieldInfo[] fields = t.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
 System.Reflection.BindingFlags.Public);
            foreach(var field in fields) {

                object valueObj;
                if(dict.TryGetValue(field.Name, out valueObj)) {
                    field.SetValue(result, DecodeJson(valueObj, field.FieldType, customSerializer));
                }
            }

            return result;
        }



        class TestSimpleClass
        {
            int _private = 5;
            public void SetPrivate(int xx) { _private = xx; }
            public int GetPrivate() { return _private; }

            public int x = 10;
            public int y = 20;
            public float floatValue = 10.0f;
            public double doubleValue = 30.0;
            public bool boolValue = false;
            public string strValue = "abc";

            public int[] myarray = new int[] { 5, 6, 7, 8, 9 };

            public List<string> mylist = new List<string>();

            public Dictionary<string, int> mydict = new Dictionary<string, int>();

            public TestClass testClass = new TestClass();

            public TestStruct testStruct = new TestStruct(5);
        }

        public class TestClass
        {
            public int x = 48;
            public TestClass recursiveTestClass = null;
        }

        class TestClassDerived : TestClass
        {
            public int y = 9;
        }

        class TestNoSerialize
        {
            public int x = 5;

            [NoSerialize]
            public int y = 9;
        }

        struct TestStruct
        {
            public TestStruct(int vv)
            {
                x = 5;
                str = "abc";
                testClass = new TestClass();
            }
            public int x;
            public string str;
            public TestClass testClass;
        }

        enum TestEnum
        {
            aa,
            bb,
            cc
        }

        public static void UnitTestScriptableObject(UnitAbility unitAbility)
        {
            string json = ToJson(unitAbility);
            Debug.Log("SCRIPTABLE_OBJECT: " + json);



        }

        public class TestHashSet
        {
            public HashSet<Loc> locs = null;
        }

        public static void UnitTest()
        {

            Debug.Log("JSON UNIT TEST...");
            Debug.Log("  TEST: 5 -> " + ToJson(5));
            Debug.Log("  TEST: 5.54 -> " + ToJson(5.54));
            Debug.Log("  TEST: 5.54f -> " + ToJson(5.54f));
            Debug.Log("  TEST: true -> " + ToJson(true));
            Debug.Log("  TEST: false -> " + ToJson(false));
            Debug.Log("  TEST: \"xx\" -> " + ToJson("xx"));

            Debug.Log("  TEST: enum(0) -> " + ToJson(TestEnum.aa));
            Debug.Log("  TEST: enum(1) -> " + ToJson(TestEnum.bb));

            List<string> items = new List<string>();
            items.Add("xx");
            items.Add("yy");
            Debug.Log("  TEST: List<string> -> " + ToJson(items));

            Debug.Log("  TEST: string[] -> " + ToJson(new string[] { "xx", "yy" }));

            Dictionary<string, int> dictStrInt = new Dictionary<string, int>();
            dictStrInt["xx"] = 5;
            dictStrInt["yy"] = 8;

            Debug.Log("  TEST: Dictionary<string,int> -> " + ToJson(dictStrInt));

            Dictionary<int, string> dictIntStr = new Dictionary<int, string>();
            dictIntStr[5] = "xx";
            dictIntStr[8] = "yy";

            Debug.Log("  TEST: Dictionary<int,string> -> " + ToJson(dictIntStr));

            Debug.Log("  TEST: TestStruct -> " + ToJson(new TestStruct(5)));

            TestClass testClass = new TestClass();
            testClass.recursiveTestClass = new TestClass();
            Debug.Log("  TEST: TestClass -> " + ToJson(testClass));

            TestNoSerialize noserialize = new TestNoSerialize();
            Debug.Log("  TEST: NoSerialize -> " + ToJson(noserialize));

            TestSimpleClass simpleClass = new TestSimpleClass();
            simpleClass.x = 100;
            simpleClass.y = 200;
            simpleClass.testClass.x = 300;
            simpleClass.myarray[2] = 27;
            simpleClass.mylist.Add("aaa");
            simpleClass.mydict["key"] = 19;
            simpleClass.testStruct.x = 18;
            simpleClass.SetPrivate(777);
            string simpleClassStr = ToJson(simpleClass);

            Debug.Log("  simpleClassStr = " + simpleClassStr);
            object simpleClassObj = BestHTTP.JSON.Json.Decode(simpleClassStr);
            simpleClass = ClassFromJson(simpleClassObj, typeof(TestSimpleClass), null) as TestSimpleClass;
            Debug.Log("  TEST PARSE SIMPLE DICT: " + simpleClass.mydict["key"] + " / CLASS: " + simpleClass.GetPrivate() + " / " + simpleClass.x + "," + simpleClass.y + "," + simpleClass.testClass.x + ", array=" + simpleClass.myarray[2] + ", mylist=" + simpleClass.mylist[0] + ", teststruct.x=" + simpleClass.testStruct.x);

            Debug.Log("  TEST LOC: " + ToJson(new Loc(4, 5)));

            HashSet<Loc> locs = new HashSet<Loc>();
            locs.Add(new Loc(4, 8));
            locs.Add(new Loc(5, 8));
            locs.Add(new Loc(6, 8));
            locs.Add(new Loc(2, 3));
            locs.Add(new Loc(8, 2));
            locs.Add(new Loc(8, 1));
            locs.Add(new Loc(7, 1));

            TestHashSet h = new TestHashSet();
            h.locs = locs;
            Debug.Log("  TEST LOC HASH: " + ToJson(h, new GameSerializer()));
            string json = ToJson(h, new GameSerializer());
            h = FromJson<TestHashSet>(json, new GameSerializer());

            Debug.Log("  TEST LOC HASH NELEMENTS = " + h.locs.Count);
            foreach(Loc loc in h.locs) {
                Debug.Log("  TEST LOC: " + loc);
            }

            if(h.locs.Count != locs.Count) {
                Debug.LogError("Test of hash locs failed: " + h.locs.Count + " vs " + locs.Count);
            }

            foreach(Loc loc in locs) {
                if(h.locs.Contains(loc) == false) {
                    Debug.LogErrorFormat("Could not find expected loc in serialize of hash locs: {0}", loc);
                }
            }
        }

    }

}