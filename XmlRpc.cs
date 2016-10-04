﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace Inedo.BuildMasterExtensions.Trac
{
    /// <summary>
    /// Simple XML-RPC proxy class.
    /// </summary>
    internal sealed class XmlRpc
    {
        private readonly Uri uri;
        private readonly string authenticationToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlRpc"/> class.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="user">The user account making the request.</param>
        /// <param name="password">The password of the user account.</param>
        public XmlRpc(Uri uri, string user, string password)
        {
            this.uri = uri;
            if(!string.IsNullOrEmpty(user))
                this.authenticationToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(user + ":" + password));
        }

        /// <summary>
        /// Invokes the specified XML-RPC method.
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <returns>Return value of the method.</returns>
        public object Invoke(string methodName)
        {
            return Invoke(methodName, (object[])null);
        }
        /// <summary>
        /// Invokes the specified XML-RPC method.
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="args">Arguments to pass to the method.</param>
        /// <returns>Return value of the method.</returns>
        public object Invoke(string methodName, params object[] args)
        {
            var buffer = new MemoryStream();
            var request = WebRequest.Create(this.uri);
            request.ContentType = "text/xml; charset=\"utf-8\"";
            request.Method = "POST";
            if (!string.IsNullOrEmpty(this.authenticationToken))
            {
                request.PreAuthenticate = true;
                request.AuthenticationLevel = System.Net.Security.AuthenticationLevel.MutualAuthRequested;
                request.Headers.Add("Authorization", "Basic " + this.authenticationToken);
            }

            var xmlWriter = XmlWriter.Create(buffer, new XmlWriterSettings() { Encoding = Encoding.UTF8 });

            xmlWriter.WriteStartDocument();

            xmlWriter.WriteStartElement("methodCall");
            xmlWriter.WriteElementString("methodName", methodName);
            xmlWriter.WriteStartElement("params");

            if (args != null && args.Length > 0)
            {
                foreach (var arg in args)
                {
                    xmlWriter.WriteStartElement("param");
                    WriteValue(xmlWriter, arg);
                    xmlWriter.WriteEndElement();
                }
            }

            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndDocument();

            xmlWriter.Flush();

            request.ContentLength = buffer.Length;
            var requestStream = request.GetRequestStream();
            requestStream.Write(buffer.ToArray(), 0, (int)buffer.Length);
            requestStream.Close();  // ?? jbuckshin - 2016.10.4

            var response = request.GetResponse();
            var doc = new XmlDocument();
            doc.Load(response.GetResponseStream());

            var responseValues = doc.GetElementsByTagName("param");
            if (responseValues.Count == 0)
                return null;
            else
                return ReadValue(responseValues[0].FirstChild);
        }

        /// <summary>
        /// Writes an XML-RPC struct.
        /// </summary>
        /// <param name="xmlWriter">The XML writer to write to.</param>
        /// <param name="pairs">The key/value pairs to write.</param>
        private void WriteStruct(XmlWriter xmlWriter, IEnumerable<KeyValuePair<string, object>> pairs)
        {
            xmlWriter.WriteStartElement("struct");
            foreach (var pair in pairs)
            {
                xmlWriter.WriteStartElement("member");
                xmlWriter.WriteElementString("name", pair.Key);
                xmlWriter.WriteStartElement("value");

                WriteValue(xmlWriter, pair.Value);

                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndElement();
        }
        /// <summary>
        /// Writes an XML-RPC value.
        /// </summary>
        /// <param name="xmlWriter">The XML writer to write to.</param>
        /// <param name="value">The value to write.</param>
        private void WriteValue(XmlWriter xmlWriter, object value)
        {
            if (value is int)
                xmlWriter.WriteElementString("i4", value.ToString());
            else if (value is bool)
                xmlWriter.WriteElementString("boolean", (bool)value ? "1" : "0");
            else if (value is double)
                xmlWriter.WriteElementString("double", value.ToString());
            else if (value is string)
                xmlWriter.WriteElementString("string", value.ToString());
            else if (value is DateTime)
                xmlWriter.WriteElementString("dateTime.iso8601", ((DateTime)value).ToString("s"));
            else if (value is IEnumerable<KeyValuePair<string, object>>)
                WriteStruct(xmlWriter, (IEnumerable<KeyValuePair<string, object>>)value);
            else if (value is System.Collections.IEnumerable)
                WriteArray(xmlWriter, (System.Collections.IEnumerable)value);
            else if (value == null)
                xmlWriter.WriteElementString("nil", null);
        }
        /// <summary>
        /// Writes an XML-RPC array.
        /// </summary>
        /// <param name="xmlWriter">The XML writer to write to.</param>
        /// <param name="array">The array to write.</param>
        private void WriteArray(XmlWriter xmlWriter, System.Collections.IEnumerable array)
        {
            xmlWriter.WriteStartElement("array");
            xmlWriter.WriteStartElement("data");
            foreach (var item in array)
            {
                xmlWriter.WriteStartElement("value");
                WriteValue(xmlWriter, item);
                xmlWriter.WriteEndElement();
            }
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();
        }
        /// <summary>
        /// Reads an XML-RPC value.
        /// </summary>
        /// <param name="valueNode">The XML value node.</param>
        /// <returns>The value read from the XML node.</returns>
        private object ReadValue(XmlNode valueNode)
        {
            var child = valueNode.FirstChild;
            if (child == null)
                return null;

            switch (child.Name)
            {
                case "i4":
                case "int":
                    return int.Parse(child.InnerText);
                case "#text":
                    int i;
                    if (int.TryParse(child.InnerText, out i))
                        return i;
                    else
                        return child.InnerText;
                case "boolean":
                    return child.InnerText != "0";
                case "double":
                    return double.Parse(child.InnerText);
                case "string":
                    return child.InnerText ?? string.Empty;
                case "dateTime.iso8601":
                    return DateTime.Parse(child.InnerText.Insert(6, "-").Insert(4, "-"));
                case "array":
                    return ReadArray(child);
                case "struct":
                    return ReadStruct(child);
                case "nil":
                    return null;
            }

            throw new ArgumentException();
        }
        /// <summary>
        /// Reads an XML-RPC array.
        /// </summary>
        /// <param name="arrayNode">The XML array node.</param>
        /// <returns>The array read from the XML node.</returns>
        private object[] ReadArray(XmlNode arrayNode)
        {
            var list = new List<object>();
            var child = arrayNode.FirstChild;
            foreach (XmlNode item in child.ChildNodes)
            {
                if (item.FirstChild.Name == "struct")
                    list.Add(ReadStruct(item.FirstChild));
                else if (item.FirstChild.Name == "array")
                    list.Add(ReadArray(item.FirstChild));
                else
                    list.Add(ReadValue(item.FirstChild));
            }

            return list.ToArray();
        }
        /// <summary>
        /// Reads an XML-RPC struct.
        /// </summary>
        /// <param name="structNode">The XML struct node.</param>
        /// <returns>The struct read from the XML node.</returns>
        private Dictionary<string, object> ReadStruct(XmlNode structNode)
        {
            var values = new Dictionary<string, object>();

            foreach (XmlNode member in structNode.ChildNodes)
                values.Add(member["name"].InnerText, ReadValue(member["value"]));

            return values;
        }
    }
}
