SmiEditor.highlightText = function(text, state=null) {
	var previewLine = $("<span>");
	if (text.toUpperCase().startsWith("<SYNC ")) {
		previewLine.addClass("sync");
	}
	return previewLine.text(text);
}