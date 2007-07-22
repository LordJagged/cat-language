/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;

namespace Cat
{
    /// <summary>
    /// A context class holds all loaded definitions
    /// </summary>
    public class Context
    {
        private Dictionary<string, Function> mpFunctions = new Dictionary<string, Function>();

        public Context()
        {
        }

        public Context(Context x)
        {
            foreach (KeyValuePair<string, Function> kvp in x.mpFunctions)
            {
                mpFunctions.Add(kvp.Key, kvp.Value);
            }
        }

        #region public functions

        public bool FunctionExists(string s)
        {
            return mpFunctions.ContainsKey(s);
        }

        public Function Lookup(ITypeArray piTypes, string s)
        {
            if (s.Length < 1)
                throw new Exception("trying to lookup a function with no name");
            if (mpFunctions.ContainsKey(s))
                return mpFunctions[s];
            else
                return null;
        }

        public Function Lookup(string s)
        {
            if (s.Length < 1)
                throw new Exception("trying to lookup a function with no name");
            
            if (mpFunctions.ContainsKey(s))
                return mpFunctions[s];
            
            // Special functions. Not primitives.
            switch (s)
            {
                case "_set_":
                    return new SetFieldFxn();
                case "_get_":
                    return new GetFieldFxn();
                case "_def_":
                    return new DefFieldFxn();
            }
            
            return null;
        }

        public void Clear()
        {
            mpFunctions.Clear();
        }

        public void AddFunction(Function f)
        {
            string s = f.GetName();
            if (mpFunctions.ContainsKey(s))
            {
                if (!Config.gbAllowRedefines)
                    throw new Exception("attempting to redefine " + s);
                mpFunctions[s] = f;
            }
            else
            {
                mpFunctions.Add(s, f);
            }
        }

        private IEnumerable<Function> GetFunctions()
        {
            return mpFunctions.Values;
        }

        private IEnumerable<Function> GetDefinedFunctions()
        {
            foreach (Function f in mpFunctions.Values)
                if (f is DefinedFunction)
                    yield return f;
        }

        private void RemoveFunction(string s)
        {
            mpFunctions.Remove(s);
        }

        /// <summary>
        /// Methods allow overloading of function definitions.
        /// </summary>
        public void AddMethod(Object o, MethodInfo mi)
        {
            if (!mi.IsPublic) 
                return;

            if (mi.IsStatic)
                o = null;
            
            Method f = new Method(o, mi);
            string s = f.GetName();
            
            if (mpFunctions.ContainsKey(s))
            {
                throw new Exception("unable to overload methods");                
            }
            else
            {
                Method g = new Method(o, mi);
                mpFunctions.Add(s, g);
            }
        }

        public void RemoveFunctions(string s)
        {
            mpFunctions.Remove(s);
        }

        public Dictionary<String, Function>.ValueCollection GetAllFunctions()
        {
            return mpFunctions.Values;
        }
        #endregion

        public void RegisterType(Type t)
        {            
            foreach (Type memberType in t.GetNestedTypes())
            {
                // Is is it a function object
                if (typeof(Function).IsAssignableFrom(memberType))
                {
                    ConstructorInfo ci = memberType.GetConstructor(new Type[] { });
                    Object o = ci.Invoke(null);
                    if (!(o is Function))
                        throw new Exception("Expected only function objects in " + t.ToString());
                    Function f = o as Function;
                    AddFunction(f);
                }
                else
                {
                    RegisterType(memberType);
                }
            }
            foreach (MemberInfo mi in t.GetMembers())
            {
                if (mi is MethodInfo) 
                {
                    MethodInfo meth = mi as MethodInfo;
                    if (meth.IsStatic)
                    {
                        AddMethod(null, meth);
                    }
                }
            }
        }

        /// <summary>
        /// Creates an ObjectBoundMethod for each public function in the object
        /// </summary>
        /// <param name="o"></param>
        public void RegisterObject(Object o)
        {
            foreach (MemberInfo mi in o.GetType().GetMembers())
            {
                if (mi is MethodInfo)
                {
                    MethodInfo meth = mi as MethodInfo;
                    AddMethod(o, meth);
                }
            }
        }

        public void UnregisterObject(Object o)
        {
            List<string> keys = new List<string>();
            foreach (KeyValuePair<string, Function> kvp in mpFunctions)
            {
                if (kvp.Value is Method)
                {
                    Method obm = kvp.Value as Method;
                    if (obm.GetObject() == o)
                    {
                        keys.Add(kvp.Key);
                    }
                }
            }

            foreach (string s in keys)
                mpFunctions.Remove(s);
        }

        public static int CompareFxns(Function f1, Function f2)
        {
            return f1.GetName().CompareTo(f2.GetName());
        }

        public void SaveToFile(string sFile)
        {
            using (StreamWriter sw = new StreamWriter(sFile))
            {
                sw.WriteLine("// session file created on " + DateTime.Now);
                sw.WriteLine();
                
                List<Function> fxns = new List<Function>(GetDefinedFunctions());
                fxns.Sort(CompareFxns);

                foreach (Function f in fxns)
                {
                    f.WriteTo(sw);
                    sw.WriteLine();
                }
                sw.Flush();
                sw.Close();
            }
        }
    }
}
