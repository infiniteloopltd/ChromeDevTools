using MasterDevs.ChromeDevTools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MasterDevs.ChromeDevTools.Protocol.Chrome.DOM
{
	/// <summary>
	/// Disables DOM agent for the given page.
	/// </summary>
	[CommandResponse(ProtocolName.DOM.Disable)]
	[SupportedBy("Chrome")]
	public class DisableCommandResponse
	{
	}
}
