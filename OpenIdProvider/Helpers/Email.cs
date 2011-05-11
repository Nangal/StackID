﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Mail;
using System.Net;
using MarkdownSharp;
using System.IO;
using System.Text;

namespace OpenIdProvider.Helpers
{
    /// <summary>
    /// When dealing with multiple concrete Email implementations,
    /// Current will use this attribute to determine which one to actually instantiate.
    /// 
    /// The highest priority (where 2 has a higher priority than 1) wins.
    /// </summary>
    public class PriorityAttribute : Attribute
    {
        public int Priority { get; set; }

        public PriorityAttribute(int priority) { Priority = priority; }
    }

    /// <summary>
    /// Helper class for sending e-mails.
    /// 
    /// Abstracts away all the nasty SMTP and
    /// message formatting non-sense.
    /// </summary>
    public abstract class Email
    {
        public enum Template
        {
            AffiliateRegistered,
            CompleteRegistration,
            CompleteRegistrationViaAffiliate,
            PasswordChanged,
            ResetPassword
        }

        /// <summary>
        /// Returns an e-mail template, with all the {Names} replaced with the corresponding properties on params and some
        /// default "SiteWide" ones like {SiteName}.
        /// 
        /// Also pushes an appropriate subject into subject.
        /// </summary>
        private static string GetEmailText(string templateName, object @params, out string subject, out string textVersion)
        {
            var markdown = Helpers.Template.FormatTemplate(templateName, @params).Trim();
            int i = markdown.IndexOf('\n');
            subject = markdown.Substring(0, i + 1).Trim();

            textVersion = markdown.Substring(i + 1).Trim();

            return (new Markdown()).Transform(textVersion);
        }

        /// <summary>
        /// Sends am e-mail.
        /// 
        /// cc and bcc accept semi-colon delimitted lists of addresses.
        /// </summary>
        public void SendEmail(string to, Template templateName, object @params = null, string cc = null, string bcc = null)
        {
            var ccList = new List<string>();
            var bccList = new List<string>();

            if (cc.HasValue()) ccList.AddRange(cc.Split(';'));
            if (bcc.HasValue()) bccList.AddRange(bcc.Split(';'));

            string subject, textMessage;
            var htmlMessage = GetEmailText(Enum.GetName(typeof(Template), templateName), @params, out subject, out textMessage);

            SendEmailImpl(to, ccList, bccList, subject, htmlMessage, textMessage);
        }

        /// <summary>
        /// Actual implementation of sending an e-mail.
        /// 
        /// Concrete implementations of this method must be thread safe.
        /// 
        /// This whole song and dance is to enable complete different implementations of email
        /// to be swapped out easily.  SE Inc. relies on some third-party, closed source, mailing
        /// libraries.
        /// </summary>
        protected abstract void SendEmailImpl(string to, IEnumerable<string> cc, IEnumerable<string> bcc, string title, string bodyHtml, string bodyText);
    }
}