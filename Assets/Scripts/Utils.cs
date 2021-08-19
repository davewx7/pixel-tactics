using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils
{
    //a mod implementation fixed for negative numbers so it always
    //returns positive values.
    static public int mod(int x, int y)
    {
        x = x%y;
        if(x < 0) {
            x = y+x;
        }

        return x;
    }

    public static string HashEmail(string email)
    {
        var md5 = System.Security.Cryptography.MD5.Create();
        byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(email.ToLower()));

        string result = "";
        for(int i = 0; i != hash.Length; ++i) {
            result += hash[i].ToString("X2");
        }

        Debug.Log("HASH EMAIL: (" + email + ") -> (" + result + ")");
        return result;
    }

    // Function which patches the object 'node' within the given 'path', either replacing
    // or updating it depending on the value of 'replace'.
    public static void PatchJson(string path, object obj, ref object node, bool replace)
    {
        if(path == "" || path == "/") {
            if(replace) {
                node = obj;
            } else {
                //patch
                Dictionary<string, object> dict = node as Dictionary<string, object>;
                List<object> list = null;
                if(dict == null) {
                    list = node as List<object>;
                    if(list == null) {
                        dict = new Dictionary<string, object>();
                        node = dict;
                    }
                }

                Dictionary<string, object> patch = obj as Dictionary<string, object>;
                if(patch != null) {
                    foreach(KeyValuePair<string, object> p in patch) {
                        if(dict != null) {
                            dict[p.Key] = p.Value;
                        } else {
                            list[int.Parse(p.Key)] = p.Value;
                        }
                    }
                }
            }
            return;
        } else {
            Dictionary<string, object> dict = node as Dictionary<string, object>;
            List<object> list = null;
            if(dict == null) {
                list = node as List<object>;
                if(list == null) {
                    dict = new Dictionary<string, object>();
                    node = dict;
                }
            }

            int beginIndex = 0;
            if(path[beginIndex] == '/') {
                ++beginIndex;
            }
            int endIndex = beginIndex;
            while(endIndex < path.Length && path[endIndex] != '/') {
                ++endIndex;
            }

            string id = path.Substring(beginIndex, endIndex - beginIndex);
            string subPath = path.Substring(endIndex);

            if(list != null) {
                int index = int.Parse(id);
                while(list.Count <= index) {
                    list.Add(null);
                }
                object value = list[index];
                PatchJson(subPath, obj, ref value, replace);
                list[index] = value;
            } else {

                object value = null;
                if(dict.ContainsKey(id)) {
                    value = dict[id];
                }

                PatchJson(subPath, obj, ref value, replace);
                dict[id] = value;
            }
        }
    }
}