using System;
using System.Linq;

namespace Wabbajack.Paths
{
    public readonly struct RelativePath : IPath, IEquatable<RelativePath>, IComparable<RelativePath>
    {
        internal readonly string[] Parts;
        internal RelativePath(string[] parts)
        {
            Parts = parts;
        }

        public static explicit operator RelativePath(string i)
        {
            var splits = i.Split(AbsolutePath.StringSplits, StringSplitOptions.RemoveEmptyEntries);
            if (splits.Length >= 1 && splits[0].Contains(":"))
                throw new PathException($"Tried to parse `{i} but `:` not valid in a path name");
            return new(splits);
        }
        
        public static explicit operator string(RelativePath i)
        {
            return i.ToString();
        }

        public Extension Extension => Extension.FromPath(Parts[^1]);
        public RelativePath FileName => Parts.Length == 1 ? this : new RelativePath(new[] {Parts[^1]});

        public RelativePath ReplaceExtension(Extension newExtension)
        {
            var paths = new string[Parts.Length];
            Array.Copy(Parts, paths, paths.Length);
            var oldName = paths[^1];
            var newName = ReplaceExtension(oldName, newExtension);
            paths[^1] = newName;
            return new RelativePath(paths);
        }

        internal static string ReplaceExtension(string oldName, Extension newExtension)
        {
            var oldExtLength = oldName.LastIndexOf(".", StringComparison.CurrentCultureIgnoreCase);
            if (oldExtLength < 0)
                oldExtLength = 0;
            else
                oldExtLength++;
            
            var newName = oldName[..^oldExtLength] + newExtension;
            return newName;
        }
        
        public RelativePath WithExtension(Extension? ext)
        {
            var parts = new string[Parts.Length];
            Array.Copy(Parts, parts, Parts.Length);
            parts[-1] = parts[-1] + ext;
            return new RelativePath(parts);
        }

        public AbsolutePath RelativeTo(AbsolutePath basePath)
        {
            var newArray = new string[basePath.Parts.Length + Parts.Length];
            Array.Copy(basePath.Parts, 0, newArray, 0, basePath.Parts.Length);
            Array.Copy(Parts, 0, newArray, basePath.Parts.Length, Parts.Length);
            return new AbsolutePath(newArray, basePath.PathFormat);
        }

        public override string ToString()
        {
            return string.Join('\\', Parts);
        }
        
        public override int GetHashCode()
        {
            return Parts.Aggregate(0, (current, part) => current ^ part.GetHashCode(StringComparison.CurrentCultureIgnoreCase));
        }

        public bool Equals(RelativePath other)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (other.Parts == default) return other.Parts == Parts;
            
            if (other.Parts.Length != Parts.Length) return false;
            for (var idx = 0; idx < Parts.Length; idx++)
            {
                if (!Parts[idx].Equals(other.Parts[idx], StringComparison.InvariantCultureIgnoreCase))
                    return false;
            }
            return true;
        }

        public override bool Equals(object? obj)
        {
            return obj is RelativePath other && Equals(other);
        }
        
        public static bool operator ==(RelativePath a, RelativePath b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(RelativePath a, RelativePath b)
        {
            return !a.Equals(b);
        }

        public int CompareTo(RelativePath other)
        {
            return ArrayExtensions.CompareString(Parts, other.Parts);
        }

        public int Depth => Parts.Length;

        public RelativePath Parent
        {
            get
            {
                if (Parts.Length <= 1)
                    throw new PathException("Can't get parent of a top level path");

                var newParts = new string[Parts.Length - 1];
                Array.Copy(Parts, newParts, Parts.Length - 1);
                return new RelativePath(newParts);
            }
        }
    }
}