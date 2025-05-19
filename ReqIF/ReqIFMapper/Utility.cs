using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using ReqIFSharp;

namespace ReqIFBridge.ReqIF.ReqIFMapper
{
    internal static class Extensions
    {
        /// <summary>
        /// Deserializes the specified value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static T Deserialize<T>(this string value)
        {
            var xmlSerializer = new XmlSerializer(typeof(T));

            return (T)xmlSerializer.Deserialize(new StringReader(value));
        }

        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string Serialize<T>(this T value, List<ReqIFSharp.ReqIF> reqIFs)
        {
            if (value == null)
                return string.Empty;

            var xmlSerializer = new XmlSerializer(typeof(T));
            
            using (var stringWriter = new StringWriter())
            {
                using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true }))
                {
                    xmlSerializer.Serialize(xmlWriter, value);
                    return stringWriter.ToString();
                }
            }
        }

        public static SpecHierarchy FindParentHierarchy(this SpecHierarchy specHierarchy)
        {
            Specification specification = specHierarchy.Root;

            if (specification.Children.Exists(child => child.Identifier == specHierarchy.Identifier))
            {
                return null;
            }

            SpecHierarchy parentSpecHierarchy = null;

            foreach (SpecHierarchy child in specification.Children)
            {
                parentSpecHierarchy = FindParentHierarchy(child, specHierarchy.Identifier);
            }

            return parentSpecHierarchy;
        }

        private static SpecHierarchy FindParentHierarchy(SpecHierarchy specHierarchy, string identifier)
        {
            if (specHierarchy.Identifier == identifier)
            {
                return null;
            }

            if (specHierarchy.Children.Exists(child => child.Identifier == identifier))
            {
                return specHierarchy;
            }

            SpecHierarchy parentSpecHierarchy = null;

            foreach (SpecHierarchy child in specHierarchy.Children)
            {
                parentSpecHierarchy = FindParentHierarchy(child, identifier);

                if(parentSpecHierarchy != null)
                { 
                    break;
                }
            }

            return parentSpecHierarchy;
        }

        public static SpecHierarchy FindSpecObject(this Specification specification, string identifier)
        {
            SpecHierarchy retVal = null;

            foreach (SpecHierarchy specHierarchy in specification.Children)
            {
                retVal = specHierarchy.FindSpecObject(identifier);

                if (retVal != null)
                {
                    break;
                }
            }

            return retVal;
        }

        public static SpecHierarchy FindSpecObject(this SpecHierarchy specHierarchy, string identifier)
        {
            if (specHierarchy.Object.Identifier == identifier)
            {
                return specHierarchy;
            }

            SpecHierarchy retVal = null;

            foreach (SpecHierarchy child in specHierarchy.Children)
            {
                retVal = child.FindSpecObject(identifier);

                if (retVal != null)
                {
                    return retVal;
                }
            }
           
            return null;
        }

        public static string StripTagsRegex(this string source)
        {
            return Regex.Replace(source, "<.*?>", string.Empty);
        }

        /// <summary>
        /// Compiled regular expression for performance.
        /// </summary>
        static Regex _htmlRegex = new Regex("<.*?>", RegexOptions.Compiled);

        /// <summary>
        /// Remove HTML from string with compiled Regex.
        /// </summary>
        public static string StripTagsRegexCompiled(string source)
        {
            return _htmlRegex.Replace(source, string.Empty);
        }

        /// <summary>
        /// Remove HTML tags from string using char array.
        /// </summary>
        public static string StripTagsCharArray(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex);
        }     

        /// <summary>
        /// Finds the tree node.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <param name="workItemId">The work item identifier.</param>
        /// <returns></returns>
        public static TreeNode<WorkItem> FindTreeNode(this Tree<WorkItem> tree, int workItemId)
        {
            TreeNode<WorkItem> retVal = null;

            foreach (TreeNode<WorkItem> rootNode in tree.RootNodes)
            {
                retVal = rootNode.FindTreeNode(workItemId);

                if (retVal != null)
                {
                    break;
                }
            }
            
            return retVal;
        }

        /// <summary>
        /// Finds the tree node.
        /// </summary>
        /// <param name="treeNode">The tree node.</param>
        /// <param name="workItemId">The work item identifier.</param>
        /// <returns></returns>
        public static TreeNode<WorkItem> FindTreeNode(this TreeNode<WorkItem> treeNode, int workItemId)
        {
            if (treeNode.Data.Id == workItemId)
            {
                return treeNode;
            }

            TreeNode<WorkItem> retVal = null;

            foreach (TreeNode<WorkItem> child in treeNode.Children)
            {
                retVal = child.FindTreeNode(workItemId);

                if (retVal != null)
                {
                    break;
                }
            }

            return retVal;
        }
    }

    public class Tree<T>
    {
        public Tree()
        {
            RootNodes = new List<TreeNode<T>>();
        }

        public List<TreeNode<T>> RootNodes { get; set; }
    }

    public class TreeNode<T>
    {
        public TreeNode()
        {
            Children = new List<TreeNode<T>>();
        }

        public TreeNode(T data, TreeNode<T> parent)
        {
            this.Data = data;
            this.Parent = parent;

            Children = new List<TreeNode<T>>();
        }

        public TreeNode<T> Parent { get; set; }

        public T Data { get; set; }

        public List<TreeNode<T>> Children { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class NoCacheAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            filterContext.HttpContext.Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            filterContext.HttpContext.Response.Cache.SetValidUntilExpires(false);
            filterContext.HttpContext.Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
            filterContext.HttpContext.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            filterContext.HttpContext.Response.Cache.SetNoStore();

            base.OnResultExecuting(filterContext);
        }
    }


    /// <summary>
    /// Field Types enum
    /// </summary>
    public enum FieldTypes
    {
        Numeric,
        String,
        Enum,
        RichText,
        DateTime
    }
}

