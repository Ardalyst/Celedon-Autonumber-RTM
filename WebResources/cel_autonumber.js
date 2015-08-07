// Author: Matt Barnes (matt.barnes@celedonpartners.com)
/*The MIT License (MIT)

Copyright (c) 2015 Celedon Partners 

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

var CeledonPartners = (function (CeledonPartners) {

	CeledonPartners.AutoNumberOnload = function onLoad() {
		if (Xrm.Page.ui.getFormType() != 1) {
			Xrm.Page.getControl("cel_entityname").setDisabled(true);
			Xrm.Page.getControl("cel_attributename").setDisabled(true);
			CeledonPartners.GeneratePreview();
		}
		Xrm.Page.getAttribute("cel_preview").setSubmitMode("never");
	}

	CeledonPartners.GeneratePreview = function generatePreview() {
		var Prefix = Xrm.Page.getAttribute("cel_prefix").getValue() || "";
		var Suffix = Xrm.Page.getAttribute("cel_suffix").getValue() || "";
		var NextNumber = Xrm.Page.getAttribute("cel_nextnumber").getValue() || 1;

		Xrm.Page.getAttribute("cel_preview").setValue(Prefix + zeroPad(NextNumber) + Suffix);
	}

	function zeroPad(num, digits) {
		digits = digits || Xrm.Page.getAttribute("cel_digits").getValue() || 1;
		return ('0000000000000000' + num).substr(-digits);
	}

	return CeledonPartners;
}(CeledonPartners || {}));
