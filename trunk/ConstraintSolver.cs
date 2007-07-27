// Public domain by Christopher Diggins
// See: http://research.microsoft.com/Users/luca/Papers/BasicTypechecking.pdf
// for information on the algorithms used.

// Luca Cardelli says: In unifying a non-generic variable to a
// term, all the type variables contained in that term become non-generic

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Cat
{
    public class Pair<T>
    {
        public T First;
        public T Second;
        public Pair(T first, T second)
        {
            First = first;
            Second = second;
        }
    }

    public class Constraint
    {
        public virtual IEnumerable<Constraint> GetSubConstraints()
        {
            yield return this;
        }

        public bool EqualsVar(string s)
        {
            if (!(this is Var))
                return false;
            return ToString().Equals(s);
        }
    }

    public class Var : Constraint
    {
        protected string m;

        public Var(string s)
        {
            m = s;
        }

        public override string ToString()
        {
            return m;
        }
    }

    public class ScalarVar : Var
    {
        public ScalarVar(string s)
            : base(s)
        {
        }
    }

    public class VectorVar : Var
    {
        public VectorVar(string s)
            : base(s)
        {
        }
    }

    public class Constant : Constraint
    {
        string m;
        public Constant(string s)
        {
            m = s;
        }
        public override string ToString()
        {
            return m;
        }
    }

    public class RecursiveRelation : Constraint
    {
        public RecursiveRelation()
        {
        }
    }

    public class Relation : Constraint
    {
        Vector mLeft;
        Vector mRight;

        public Relation(Vector left, Vector right)
        {
            mLeft = left;
            mRight = right;
        }

        public Vector GetLeft()
        {
            return mLeft;
        }

        public Vector GetRight()
        {
            return mRight;
        }

        public override string ToString()
        {
            return mLeft.ToString() + "->" + mRight.ToString();
        }

        public IEnumerable<Relation> GetChildRelations()
        {
            foreach (Constraint c in GetChildConstraints())
                if (c is Relation)
                    yield return c as Relation;
        }

        public IEnumerable<Constraint> GetChildConstraints()
        {
            foreach (Constraint c in mLeft)
                yield return c;
            foreach (Constraint c in mRight)
                yield return c;
        }

        public override IEnumerable<Constraint> GetSubConstraints()
        {
            foreach (Constraint c in mLeft.GetSubConstraints())
                yield return c;
            foreach (Constraint c in mRight.GetSubConstraints())
                yield return c;
        }

        public List<Var> GetAllVars()
        {
            List<Var> vars = new List<Var>();
            foreach (Constraint c in GetSubConstraints())
            {
                if (c is Var)
                {
                    Var v = c as Var;
                    if (!vars.Contains(v))
                        vars.Add(v);
                }
            }
            return vars;
        }

        public List<Var> GetGenericVars(List<Var> nongenerics)
        {
            List<Var> list = GetAllVars();
            List<Var> ret = new List<Var>();
            foreach (Var v in list)
            {
                if (!nongenerics.Contains(v))
                    ret.Add(v);
            }
            return ret;
        }

        public bool IsSubRelation(Relation r)
        {
            foreach (Constraint c in GetSubConstraints())
                if (c == r)
                    return true;
            return false;
        }
    }

    public class Vector : Constraint
    {
        List<Constraint> mList;

        public Vector(IEnumerable<Constraint> list)
        {
            mList = new List<Constraint>(list);
        }

        public Vector(List<Constraint> list)
        {
            mList = list;
        }

        public Vector()
        {
            mList = new List<Constraint>();
        }

        public Constraint GetFirst()
        {
            return mList[0];
        }

        public Vector GetRest()
        {
            return new Vector(mList.GetRange(1, mList.Count - 1));
        }
        
        public override string ToString()
        {
            string ret = "(";
            for (int i = 0; i < mList.Count; ++i)
            {
                if (i > 0) ret += ",";
                ret += mList[i].ToString();
            }
            ret += ")";
            return ret;
        }

        public List<Constraint> GetList()
        {
            return mList;
        }

        public bool IsEmpty()
        {
            return mList.Count == 0;
        }

        public int GetCount()
        {
            return mList.Count;
        }

        public override IEnumerable<Constraint> GetSubConstraints()
        {
            foreach (Constraint c in mList)
                foreach (Constraint d in c.GetSubConstraints())
                    yield return d;
        }

        public int GetSubConstraintCount()
        {
            int ret = 0;
            foreach (Constraint c in GetSubConstraints())
                ++ret;
            return ret;
        }

        public void Add(Constraint c)
        {
            mList.Add(c);
        }

        public IEnumerator<Constraint> GetEnumerator()
        {
            return mList.GetEnumerator();
        }
    }

    public class ConstraintList : List<Constraint>
    {
        Constraint mUnifier;

        public ConstraintList(IEnumerable<Constraint> a)
            : base(a)
        { }

        public ConstraintList()
            : base()
        { }

        public override string ToString()
        {
            string ret = "";
            for (int i = 0; i < Count; ++i)
            {
                if (i > 0) ret += " = ";
                ret += this[i].ToString();
            }
            if (mUnifier != null)
                ret += "; unifier = " + mUnifier.ToString();
            return ret;
        }

        public Vector ChooseBetterVectorUnifier(Vector v1, Vector v2)
        {
            if (v1.GetCount() > v2.GetCount())
            {
                return v1;
            }
            else if (v1.GetCount() < v2.GetCount())
            {
                return v2;
            }
            else if (v1.GetSubConstraintCount() >= v2.GetSubConstraintCount())
            {
                return v1;
            }
            else
            {
                return v2;
            }
        }

        public Constraint ChooseBetterUnifier(Constraint c1, Constraint c2)
        {
            Trace.Assert(c1 != null);
            Trace.Assert(c2 != null);
            Trace.Assert(c1 != c2);

            if (c1 is RecursiveRelation || c2 is RecursiveRelation)
            {
                return new RecursiveRelation();
            }
            else if (c1 is Var)
            {
                return c2;
            }
            else if (c2 is Var)
            {
                return c1;
            }
            else if (c1 is Constant)
            {
                return c1;
            }
            else if (c2 is Constant)
            {
                return c2;
            }
            else if ((c1 is Vector) && (c2 is Vector))
            {
                return ChooseBetterVectorUnifier(c1 as Vector, c2 as Vector);
            }
            else
            {
                return c1;
            }
        }

        public void ComputeUnifier()
        {
            if (Count == 0)
                throw new Exception("Can not compute unifier for an empty list");
            mUnifier = this[0];
            for (int i = 1; i < Count; ++i)
                mUnifier = ChooseBetterUnifier(mUnifier, this[i]);
        }

        public Constraint GetUnifier()
        {
            if (mUnifier == null)
                throw new Exception("Unifier hasn't been computed yet");
            return mUnifier;
        }

        public bool ContainsVar(string s)
        {
            foreach (Constraint c in this)
                if (c.EqualsVar(s))
                    return true;
            return false;
        }
    }

    public class ConstraintSolver
    {
        List<ConstraintList> mConstraintList; // left null, because it is created upon request
        Dictionary<string, ConstraintList> mLookup = new Dictionary<string, ConstraintList>();
        List<Pair<Vector>> mConstraintQueue = new List<Pair<Vector>>();
        Dictionary<string, Var> mVarPool = new Dictionary<string, Var>();
        int mnId = 0;

        public Var CreateUniqueVar(string s)
        {
            int n = s.IndexOf("$");
            if (n > 0)
                s = s.Substring(0, n);
            return CreateVar(s + "$" + mnId++.ToString());
        }

        public Var CreateVar(string s)
        {
            Trace.Assert(s.Length > 0);
            if (!mVarPool.ContainsKey(s))
            {
                Var v = char.IsUpper(s[0]) 
                    ? new VectorVar(s) as Var 
                    : new ScalarVar(s) as Var;
                mVarPool.Add(s, v);
                return v;
            }
            else
            {
                return mVarPool[s];
            }
        }

        public void CheckConstraintQueueEmpty()
        {
            Check(mConstraintQueue.Count == 0, "constraint queue is not empty");
        }

        ConstraintList GetConstraints(string s)
        {
            if (!mLookup.ContainsKey(s))
            {
                ConstraintList a = new ConstraintList();                
                a.Add(CreateVar(s));
                mLookup.Add(s, a);
            }

            Trace.Assert(mLookup[s].ContainsVar(s));
            return mLookup[s];
        }

        public static T Last<T>(List<T> a)
        {
            return a[a.Count - 1];
        }

        public static Pair<Vector> VecPair(Vector v1, Vector v2)
        {
            return new Pair<Vector>(v1, v2);
        }

        public void AddToConstraintQueue(Vector v1, Vector v2)
        {
            // Don't add redundnant things to the constraint queue
            foreach (Pair<Vector> pair in mConstraintQueue)
                if ((pair.First == v1) && (pair.Second == v2))
                    return;

            Log("adding to constraint queue: " + v1.ToString() + " = " + v2.ToString());
            mConstraintQueue.Add(new Pair<Vector>(v1, v2));
        }

        public void AddSubConstraints(Constraint c1, Constraint c2)
        {
            if (!(c1 is Vector)) return;
            if (!(c2 is Vector)) return;
            if (c1 == c2) return;
            AddToConstraintQueue(c1 as Vector, c2 as Vector);
        }

        public void AddVarConstraint(Var v, Constraint c)
        {
            AddConstraintToList(c, GetConstraints(v.ToString()));
        }

        public void AddRelConstraint(Relation r1, Relation r2)
        {
            AddVecConstraint(r1.GetLeft(), r2.GetLeft());
            AddVecConstraint(r1.GetRight(), r2.GetRight());
        }

        public void AddVecConstraint(Vector v1, Vector v2)
        {
            if (v1 == v2) 
                return;

            if (v1.IsEmpty() && v2.IsEmpty())
                return;

            if (v1.IsEmpty() || v2.IsEmpty())
                Err("Incompatible length vectors");

            Log("Constraining vector: " + v1.ToString() + " = " + v2.ToString());

            if (v1.GetFirst() is VectorVar)
            {
                Check(v1.GetCount() == 1, "Vector variables can only occur in the last position of a vector");
                if (v2.GetFirst() is VectorVar)
                {
                    Check(v2.GetCount() == 1, "Vector variables can only occur in the last position of a vector");
                    ConstrainVars(v1.GetFirst() as Var, v2.GetFirst() as Var);
                }
                else
                {
                    AddVarConstraint(v1.GetFirst() as VectorVar, v2);
                }
            }
            else if (v2.GetFirst() is VectorVar)
            {
                Trace.Assert(!(v1.GetFirst() is VectorVar));
                Check(v2.GetCount() == 1, "Vector variables can only occur in the last position of a vector");
                AddVarConstraint(v2.GetFirst() as VectorVar, v1);
            }
            else 
            {
                AddConstraint(v1.GetFirst(), v2.GetFirst());

                // Recursive call
                AddVecConstraint(v1.GetRest(), v2.GetRest());

                ResolveQueuedItems();
            }
        }

        public void AddConstraintToList(Constraint c, ConstraintList a)
        {
            if (a.Contains(c))
                return;
            if (c is Var)
            {
                // Update the constraint list associated with this particular variable
                // to now be "a". 
                string sVar = c.ToString();
                Trace.Assert(mLookup.ContainsKey(sVar));
                ConstraintList list = mLookup[sVar];
                if (list == a)             
                    Err("Internal error, expected constraint list to contain constraint " + c.ToString());
                mLookup[sVar] = a;
            }
            foreach (Constraint k in a)
                AddSubConstraints(k, c);
            a.Add(c);
        }

        public void ConstrainVars(Var v1, Var v2)
        {
            Check(
                ((v1 is ScalarVar) && (v2 is ScalarVar)) ||
                ((v1 is VectorVar) && (v2 is VectorVar)),
                "Incompatible variable kinds " + v1.ToString() + " and " + v2.ToString());

            ConstrainVars(v1.ToString(), v2.ToString());
        }

        public void ResolveQueuedItems()
        {
            // While we have items left in the queue to merge, we merge them
            while (mConstraintQueue.Count > 0)
            {
                Pair<Vector> p = mConstraintQueue[0];
                mConstraintQueue.RemoveAt(0);
                Log("Constraining queue item");
                AddVecConstraint(p.First, p.Second);
            }
        }

        public void ConstrainVars(string s1, string s2)
        {
            if (s1.Equals(s2))
                return;
            ConstraintList a1 = GetConstraints(s1);
            ConstraintList a2 = GetConstraints(s2);
            if (a1 == a2)
                return;

            Trace.Assert(a1 != null);
            Trace.Assert(a2 != null);

            Log("Constraining var: " + s1 + " = " + s2);
            
            foreach (Constraint c in a2)
                AddConstraintToList(c, a1);

            ResolveQueuedItems();
        }

        public static void Err(string s)
        {
            throw new Exception(s);
        }

        public static void Check(bool b, string s)
        {
            if (!b)
                Err(s);
        }

        public static void Log(string s)
        {
            if (Config.gbVerboseInference)
                Output.WriteLine(s);
        }

        public void AddDeduction(Constant c, Constraint x)
        {
            // TODO: manage some kind of deduction list
            Log(c.ToString() + " = " + x.ToString());
        }

        public void AddConstraint(Constraint c1, Constraint c2)
        {
            if (c1 == c2)
                return;

            if ((c1 is Var) && (c2 is Var))
                ConstrainVars(c1 as Var, c2 as Var);
            else if (c1 is Var)
                AddVarConstraint(c1 as Var, c2);
            else if (c2 is Var)
                AddVarConstraint(c2 as Var, c1);
            else if ((c1 is Vector) && (c2 is Vector))
                AddVecConstraint(c1 as Vector, c2 as Vector);
            else if ((c1 is Relation) && (c2 is Relation))
                AddRelConstraint(c1 as Relation, c2 as Relation);

            if (c1 is Constant)
                AddDeduction(c1 as Constant, c2);
            if (c2 is Constant)
                AddDeduction(c2 as Constant, c1);               
        }

        /// <summary>
        /// Constructs the list of unique constraint lists
        /// </summary>
        public void ComputeConstraintLists()
        {
            mConstraintList = new List<ConstraintList>();
            foreach (ConstraintList list in mLookup.Values)
                if (!mConstraintList.Contains(list))
                    mConstraintList.Add(list);
        }

        public List<ConstraintList> GetConstraintLists()
        {
            if (mConstraintList == null)
                throw new Exception("constraint lists haven't been computed");
            return mConstraintList;
        }

        public IEnumerable<string> GetConstrainedVars()
        {
            return mLookup.Keys;
        }

        public IEnumerable<string> GetAllVars()
        {
            return mVarPool.Keys;
        }

        /*
        public bool IsRecursiveRelation(string s, Constraint c)
        {
            Relation rel = c as Relation;
            if (rel == null)
                return false;
            foreach (Constraint tmp in rel.GetLeft())
                if (tmp.EqualsVar(s))
                    return true;
            foreach (Constraint tmp in rel.GetRight())
                if (tmp.EqualsVar(s))
                    return true;
            return false;
        }
        */

        public void ComputeUnifiers()
        {
            foreach (ConstraintList list in GetConstraintLists())
            {
                Trace.Assert(list.Count > 0);
                list.ComputeUnifier();
            }        
        }

        /// <summary>
        /// Gets the types variables for this relation's binding
        /// </summary>
        public void GetBoundVars(Relation r, List<Var> vars)
        {
            GetBoundVars(r.GetLeft(), vars);
            GetBoundVars(r.GetRight(), vars);
        }

        public void GetAllVarsAndUnifiers(Constraint c, List<Var> vars, Stack<Constraint> visited)
        {
            if (c == null) 
                return;
            if (visited.Contains(c))
                return;

            visited.Push(c);
            if (c is Var)
            {
                Var v = c as Var;
                if (!vars.Contains(v))
                    vars.Add(v);
                GetAllVarsAndUnifiers(GetUnifierFor(v), vars, visited);
            }
            else if (c is Vector)
            {
                Vector vec = c as Vector;
                foreach (Constraint tmp in vec)
                    GetAllVarsAndUnifiers(tmp, vars, visited);
            }
            else if (c is Relation)
            {
                Relation rel = c as Relation;
                GetAllVarsAndUnifiers(rel.GetLeft(), vars, visited);
                GetAllVarsAndUnifiers(rel.GetRight(), vars, visited);
            }
            visited.Pop();
        }

        public void GetBoundVars(Vector vec, List<Var> vars)
        {
            foreach (Constraint c in vec)
            {
                if (c is Var)
                {
                    Var v = c as Var;

                    if (!vars.Contains(v))
                        vars.Add(v);
                    Constraint u = GetUnifierFor(v);
                    if (u is Var)
                    {
                        if (!vars.Contains(u as Var))
                            vars.Add(u as Var);
                    }
                    else if (u is Vector)
                    {
                        GetBoundVars(u as Vector, vars);
                    }
                }
                else if (c is Vector)
                {
                    GetBoundVars(c as Vector, vars);
                }
                else if (c is Relation)
                {
                    Relation rel = c as Relation;
                    GetBoundVars(rel.GetLeft(), vars);
                    GetBoundVars(rel.GetRight(), vars);
                }
            }
        }

        /// <summary>
        /// This takes a unifier and replaces all variables with their unifiers.
        /// </summary>
        public Constraint Resolve(Constraint c, Stack<Constraint> visited, List<Var> nonGenerics)
        {
            // TODO: I think I need a context: who are we resolving for.
            // Aren't we always resolving variables for a particular function.

            if (visited.Contains(c)) 
            {
                return c;
            }

            visited.Push(c);
            Constraint ret;
            if (c is Var)
            {
                Var v = c as Var;
                ret = GetUnifierFor(v);
                if (ret == null)
                {
                    ret = c;
                }
                else if (ret == c)
                {
                    // do nothing
                }
                else if (ret is Var)
                {
                    Trace.Assert(GetUnifierFor(ret as Var) == ret);
                }
                else if (ret is Vector)
                {
                    ret = Resolve(ret, visited, nonGenerics);
                }
                else if (ret is Relation)
                {
                    Relation rel = ret as Relation;

                    // Resolve the relation
                    List<Var> newNonGenerics = new List<Var>(nonGenerics);
                    if (nonGenerics.Contains(v))
                    {
                        foreach (Var tmp in rel.GetAllVars())
                        {
                            if (!newNonGenerics.Contains(tmp))
                                newNonGenerics.Add(tmp);
                        }
                    }
                    rel = Resolve(rel, visited, newNonGenerics) as Relation;

                    // Rename variables in the relation
                    Dictionary<Var, Var> newNames = new Dictionary<Var, Var>();
                    List<Var> generics = rel.GetGenericVars(nonGenerics);
                    if (Config.gbVerboseInference)
                    {
                        Log("Non-generic variables");
                        foreach (Var tmp in nonGenerics)
                            Log(tmp.ToString());
                        Log("Generic variables");
                        foreach (Var tmp in generics)
                            Log(tmp.ToString());
                    }
                    foreach (Var tmp in generics)
                        newNames.Add(tmp, CreateUniqueVar(tmp.ToString()));
                    rel = RenameVars(rel, newNames) as Relation;
                    Log("Renamed relation");
                    Log(ret.ToString() + " to " + rel.ToString());
                    ret = rel;
                }
                else if (ret is Constant)
                {
                    // do nothing
                }
                else if (ret is RecursiveRelation)
                {
                    // do nothing
                }
                else
                {
                    Err("Unhandled constraint " + ret.ToString());
                }
                //Log("Resolved var");
                //Log(c.ToString() + " to " + ret.ToString());
            }
            else if (c is Vector)
            {
                Log(c.ToString());
                Vector vec = new Vector();
                foreach (Constraint tmp in (c as Vector))
                    vec.Add(Resolve(tmp, visited, nonGenerics));
                ret = vec;
                //Log("Resolved vector");
                //Log(c.ToString() + " to " + ret.ToString());
            }
            else if (c is Relation)
            {
                Relation rel = c as Relation;
                List<Var> newNonGenerics = new List<Var>(nonGenerics);
                GetBoundVars(rel, newNonGenerics);
                Vector vLeft = Resolve(rel.GetLeft(), visited, newNonGenerics) as Vector;
                Vector vRight = Resolve(rel.GetRight(), visited, newNonGenerics) as Vector;
                ret = new Relation(vLeft, vRight);
                //Log("Resolved relation");
                //Log(rel.ToString() + " to " + ret.ToString());
            }
            else
            {
                ret = c;
            }
            visited.Pop();
            Trace.Assert(ret != null);
            return ret;
        }

        public bool IsConstrained(string s )
        {
            return mLookup.ContainsKey(s);
        }

        public Constraint GetUnifierFor(Var v)
        {
            Constraint ret = GetUnifierFor(v.ToString());
            if (ret == null) return v;
            return ret;
        }

        public Constraint GetUnifierFor(string s)
        {
            if (!IsConstrained(s))
                return null;
            return mLookup[s].GetUnifier();            
        }

        public Constraint GetResolvedUnifierFor(string s)
        {
            Constraint ret = GetUnifierFor(s);
            Check(ret != null, "internal error, no unifier found for " + s);
            return Resolve(ret, new Stack<Constraint>(), new List<Var>());
        }

        public void LogConstraints()
        {
            foreach (ConstraintList list in GetConstraintLists())
                Log(list.ToString());
        }

        public Constraint RenameVars(Constraint c, Dictionary<Var, Var> vars)
        {
            if (vars.Count == 0)
                return c;

            if (c is Vector)
            {
                return RenameVars(c as Vector, vars);
            }
            else if (c is Relation)
            {
                return RenameVars(c as Relation, vars);
            }
            else if (c is Var)
            {
                Var v = c as Var;
                if (vars.ContainsKey(v))
                {
                    return vars[v];
                }
                else
                {
                    return v;
                }
            }
            else
            {
                return c;
            }
        }

        public Vector RenameVars(Vector vec, Dictionary<Var, Var> vars)
        {
            Vector ret = new Vector();
            foreach (Constraint c in vec)
                ret.Add(RenameVars(c, vars));
            return ret;
        }

        public Relation RenameVars(Relation rel, Dictionary<Var, Var> vars)
        {
            Vector vLeft = RenameVars(rel.GetLeft(), vars);
            Vector vRight = RenameVars(rel.GetRight(), vars);
            return new Relation(vLeft, vRight);
        }

        public void AddTopLevelConstraints(Vector vLeft, Vector vRight)
        {
            AddVecConstraint(vLeft, vRight);
        }

        public IEnumerable<Constraint> GetUnifiers()
        {
            foreach (ConstraintList list in GetConstraintLists())
                yield return list.GetUnifier();
        }

        public IEnumerable<Relation> GetRelationUnifiers()
        {
            foreach (Constraint c in GetUnifiers())
                if (c is Relation)
                    yield return c as Relation;
        }

        public List<Var> GetVars(Constraint c)
        {
            List<Var> list = new List<Var>();
            foreach (Constraint tmp in c.GetSubConstraints())
            {
                if (tmp is Var)
                {
                    Var v = tmp as Var;
                    if (!list.Contains(v))
                        list.Add(v);
                }
            }
            return list;
        }

        public void OutputFreeVars()
        {
            Dictionary<Relation, List<Var>> dict = new Dictionary<Relation,List<Var>>();
            foreach (Relation r in GetRelationUnifiers())
                dict.Add(r, GetVars(r));

        }
    }
}