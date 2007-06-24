/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Cat
{
    public class TypeInferer
    {
        Unifiers mUnifiers = new Unifiers();

        static TypeInferer gInferer = new TypeInferer();

        public static CatFxnType Infer(CatFxnType f, CatFxnType g, bool bVerbose, bool bCheck)
        {
            if (f == null) return null;
            if (g == null) return null;
            return gInferer.InferType(f, g, bVerbose, bCheck);
        }

        public static CatFxnType Infer(List<Function> f, bool bVerbose, bool bCheck)
        {
            if (f.Count == 0)
            {
                if (bVerbose)
                    MainClass.WriteLine("type is ( -> )");
                return CatFxnType.Create("( -> )");
            }
            else if (f.Count == 1)
            {
                Function x = f[0];

                if (bVerbose)
                    MainClass.WriteLine("type of " + x.msName + " is " + x.GetTypeString());
                return x.GetFxnType();
            }
            else
            {
                Function x = f[0];
                CatFxnType ft = x.GetFxnType();
                if (bVerbose)
                    MainClass.WriteLine("initial term = " + x.GetName() + " : " + x.GetTypeString());

                for (int i = 1; i < f.Count; ++i)
                {
                    if (ft == null)
                        return ft;
                    Function y = f[i];
                    if (bVerbose)
                    {
                        MainClass.WriteLine("Composing accumulated terms with next term");
                        MainClass.Write("previous terms = { ");
                        for (int j = 0; j < i; ++j)
                            MainClass.Write(f[j].GetName() + " ");
                        MainClass.WriteLine("} : " + ft.ToString());
                        MainClass.WriteLine("next term = " + y.GetName() + " : " + y.GetTypeString());
                    }

                    ft = TypeInferer.Infer(ft, y.GetFxnType(), bVerbose, bCheck);
                    
                    if (ft == null)
                        return null;
                }
                return ft;
            }
        }

        /// <summary>
        /// A composed function satisfy the type equation 
        /// 
        ///   ('A -> 'B) ('C -> 'D) compose : ('A -> 'D) with constraints ('B == 'C)
        /// 
        /// This makes the raw type trivial to determine, but the result isn't helpful 
        /// because 'D is not expressed in terms of the variables of 'A. The goal of 
        /// type inference is to find new variables that unify 'A and 'C based on the 
        /// observation that the production of the left function must be equal to the 
        /// consumption of the second function
        /// </summary>
        private CatFxnType InferType(CatFxnType left, CatFxnType right, bool bVerbose, bool bCheck)
        {
            mUnifiers.Clear();
            VarRenamer renamer = new VarRenamer();
            left = renamer.Rename(left);
            renamer.ResetNames(); 
            right = renamer.Rename(right);

            if (bVerbose)
            {
                MainClass.WriteLine("Types renamed before composing");
                MainClass.WriteLine("left term : " + left.ToString());
                MainClass.WriteLine("right term : " + right.ToString());
            }

            mUnifiers.AddVectorConstraint(left.GetProd(), right.GetCons());

            if (bVerbose)
            {
                // Create a temporary function type showing the type before unfification
                CatFxnType tmp = new CatFxnType(left.GetCons(), right.GetProd(), left.HasSideEffects() || right.HasSideEffects());
                MainClass.WriteLine("Result of composition before unification: ");
                MainClass.WriteLine(tmp.ToString());

                MainClass.WriteLine("Constraints:");
                MainClass.WriteLine(left.GetProd() + " = " + right.GetCons());
                MainClass.WriteLine(mUnifiers.ToString());
            }

            Dictionary<string, CatKind> unifiers = mUnifiers.GetResolvedUnifiers();
            renamer = new VarRenamer(unifiers);

            if (bVerbose)
            {
                MainClass.WriteLine("Unifiers:");
                foreach (KeyValuePair<string, CatKind> kvp in unifiers)
                    MainClass.WriteLine(kvp.Key + " = " + kvp.Value.ToString());
            }                

            // The left consumption and right production make up the result type.
            CatTypeVector stkLeftCons = renamer.Rename(left.GetCons());
            CatTypeVector stkRightProd = renamer.Rename(right.GetProd());
            
            // Finally create and return the result type
            CatFxnType ret = new CatFxnType(stkLeftCons, stkRightProd, left.HasSideEffects() || right.HasSideEffects());

            if (bVerbose)
            {
                MainClass.WriteLine("Inferred type (before renaming): " + ret.ToString());
            }

            // And one last renaming for good measure:
            renamer = new VarRenamer();
            ret = renamer.Rename(ret);

            if (bVerbose)
            {
                MainClass.WriteLine("Inferred type: " + ret.ToString());
                MainClass.WriteLine("");
            }

            if (bCheck)
                ret.CheckIfWellTyped();

            return ret;
        }

        public void OutputUnifiers(Dictionary<string, CatKind> unifiers)
        {
            MainClass.WriteLine("Unifiers:");
            foreach (KeyValuePair<string, CatKind> kvp in unifiers)
                MainClass.WriteLine(kvp.Key + " = " + kvp.Value.ToString());
        }
    }
}
