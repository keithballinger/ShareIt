using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using Mono.TextEditor;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Gtk;


namespace ShareIt
{
	public class ShareCodeHandler : CommandHandler
	{
		protected override void Run ()
		{
			Document doc = IdeApp.Workbench.ActiveDocument;
			var textEditorData = doc.GetContent<ITextEditorDataProvider> ().GetTextEditorData ();  

			String result = PostSelection(textEditorData.SelectedText);

			if (!String.IsNullOrWhiteSpace (result)) 
			{
				var clipboard = Clipboard.Get (Gdk.Selection.Clipboard);
				clipboard.Text = result;
				MessageBox.Show ("Your gist has been created. URL: \r\n" + result + "\r\n The clipboard also has this URL.");
			} 
			else 
			{
				MessageBox.Show ("Something went wrong creating the gist.", Gtk.MessageType.Error);
			}

		}

		protected override void Update (CommandInfo info)
		{
			Document doc = IdeApp.Workbench.ActiveDocument;  
			info.Enabled = doc != null && doc.GetContent<ITextEditorDataProvider> () != null;  
		} 

		/// <summary>
		/// Posts the selection to Github as a public, anonymous gist.
		/// 
		/// HACK: rewrite this to not be so hacky. Make the JSON request using an actual object, consider
		///       using WebClient or whatever the new HTTP client hotness is these days.
		/// </summary>
		/// <returns>The HTML URL for the gist.</returns>
		/// <param name="selection">Selection.</param>
		public String PostSelection(String selection)
		{
			// Seriously, this is just some messed up random code I found on StackOverflow
			String jsonMessage = "{ \"description\": \"Shared from ShareIt Xamarin Studio Add-In\",  \"public\": true,"
				+ "\"files\": {   \"file1.txt\": {"
				+ "\"content\":"
				+ JsonConvert.SerializeObject (selection)
				+ "} }}";


			String _url = "https://api.github.com/gists";

			HttpWebRequest req = WebRequest.Create(new Uri(_url)) as HttpWebRequest;
			req.UserAgent = "ShareIt Monodevelop Addin";
			req.Method = "POST";
			req.ContentType = "application/json";
			StreamWriter writer = new StreamWriter(req.GetRequestStream());

			System.Diagnostics.Debug.WriteLine(jsonMessage);
			writer.Write(jsonMessage);
			writer.Close();

			string result = null;
			using (HttpWebResponse resp = req.GetResponse() as HttpWebResponse)//Exception here
			{
				StreamReader reader = new StreamReader(resp.GetResponseStream());
				result = reader.ReadToEnd();
				System.Diagnostics.Debug.WriteLine(result);

				Dictionary<string, System.Object> values = JsonConvert.DeserializeObject<Dictionary<string, System.Object>>(result);
				return values ["html_url"] as String;
			}
		}
	}
}

