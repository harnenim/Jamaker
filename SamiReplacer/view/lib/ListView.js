﻿var ListView = function(div) {
	var self = this;
	this.area = div.empty().addClass("list-selectable");
	div.append(this.view = $("<ol>"));
	this.width = 0;
	this.list = [];
	this.clearSelection(true);
	div.attr({
			tabindex: "0"
		,	onselectstart: "return false;"
	});
	if (!div.attr("id")) {
		div.attr("id", "id_" + Math.random());
	}
	
	div.on("click", function() {
		//self.clearSelection();
	});
	div.on("click", "li", function(e) {
		e.stopPropagation();
		e.preventDefault();
		self.onClick($(this).data("index"), e.originalEvent);
	});
	div.on("keydown", function(e) {
		switch (e.keyCode) {
			case 38: { // ↑
				self.onCursorMove(-1, e.originalEvent);
				break;
			}
			case 40: { // ↓
				self.onCursorMove(+1, e.originalEvent);
				break;
			}
			case 36: { // Home
				if (self.list.length) {
					self.onSelect(0, e.originalEvent);
				}
				break;
			}
			case 35: { // End
				if (self.list.length) {
					self.onSelect(self.list.length - 1, e.originalEvent);
				}
				break;
			}
			case 32: { // Spacebar
				self.select(self.cursor, e.originalEvent.ctrlKey);
				break;
			}
			case 46: { // Delete
				self.remove();
				break;
			}
			case 65: { // A
				if (e.ctrlKey) { // Ctrl+A
					for (var i = 0; i < self.list.length; i++) {
						var item = self.list[i];
						item.li.addClass("selected");
						item.selected = true;
					}
				}
				break;
			}
			case 16: { // Shift
				if (self.shiftFrom < 0) {
					self.shiftFrom = self.cursor;
				}
			}
		}
	});
	
	var draggableArea = [0,0,0,0];
	var dragLayer = null;
	var dragFrom = [0,0];
	div.on("mousedown", function(e) {
		var offset = self.area.offset();
		draggableArea = [ offset.left, offset.top, self.area.outerWidth(), self.area.outerHeight() ];
		
		if (ListView.dragLayer) {
			dragLayer = ListView.dragLayer;
		} else {
			$("body").append(ListView.dragLayer = dragLayer = $("<div class='selection'>"));
		}
		dragFrom = [ e.clientX, e.clientY, (e.originalEvent.ctrlKey || e.originalEvent.shiftKey) ];
	});
	function showDragLayer(clientX, clientY) {
		var x = Math.min(clientX, dragFrom[0]);
		var y = Math.min(clientY, dragFrom[1]);
		var w = (clientX < dragFrom[0] ? dragFrom[0] - clientX : clientX - dragFrom[0]);
		var h = (clientY < dragFrom[1] ? dragFrom[1] - clientY : clientY - dragFrom[1]);
		
		if (x < draggableArea[0]) {
			w -= (draggableArea[0] - x);
			x = draggableArea[0];
		}
		if (y < draggableArea[1]) {
			h -= (draggableArea[1] - y);
			y = draggableArea[1];
		}
		if (x + w > draggableArea[0] + draggableArea[2]) {
			w = draggableArea[0] + draggableArea[2] - x;
		}
		if (y + h > draggableArea[1] + draggableArea[3]) {
			h = draggableArea[1] + draggableArea[3] - y;
		}
		
		dragLayer.css({
				top   : y + "px"
			,	left  : x + "px"
			,	width : w + "px"
			,	height: h + "px"
		});
		if (dragLayer.css("display") == "none") {
			if (!dragFrom[2]) { // Ctrl/Shift 안 눌렀으면 선택 해제
				for (var i = 0; i < self.list.length; i++) {
					var item = self.list[i];
					if (item.selected) {
						item.li.removeClass("selected");
						item.selected = false;
					}
				}
			}
			dragLayer.show();
		}
		
		for (var i = 0; i < self.list.length; i++) {
			var li = self.list[i].li;
			var top = li.offset().top;
			var bottom = top + li.outerHeight();
			if (bottom >= y && top <= y + h) {
				li.addClass("dragging");
			} else {
				li.removeClass("dragging");
			}
		}
	}
	$(document).on("mousemove", function(e) {
		if (!dragLayer) return;
		showDragLayer(e.clientX, e.clientY);
		
	}).on("mouseup", function(e) {
		if (!dragLayer) return;
		e.stopPropagation();
		e.preventDefault();
		
		for (var i = 0; i < self.list.length; i++) {
			var item = self.list[i];
   			if (item.li.hasClass("dragging")) {
   				item.li.removeClass("dragging");
   				if (!item.selected) {
   					item.li.addClass("selected");
   					item.selected = true;
   				}
   			}
		}
		dragLayer.hide();
		dragLayer = null;
	});
	
	div.on("dblclick", "li", function() {
		if (self.run) {
			self.run(self.list[$(this).data("index")]);
		}
	});
}
ListView.prototype.add = function(value, checkDuplication) {
	if (checkDuplication) {
		for (var i = 0; i < this.list.length; i++) {
			if (this.list[i].value == value) {
				return;
			}
		}
	}
	var span = $("<span>").text(value);
	var li = $("<li>").append(span);
	var item = { li: li, value: value, selected: false };
	this.view.append(li);
	this.list.push(item);
	this.clearSelection();
	var lv = this;
	setTimeout(function() {
		if ((item.width = span.width()) > lv.width) {
			lv.view.css({ minWidth: (lv.width = item.width) + "px" });
		}
	}, 1);
}
ListView.prototype.remove = function() {
	var list = [];
	var cursor = this.cursor;
	for (var i = 0; i < this.list.length; i++) {
		if (this.list[i].selected) {
			this.list[i].li.remove();
			if (i == cursor) {
				cursor++;
			}
		} else {
			list.push(this.list[i]);
		}
	}
	if (cursor >= this.list.length) {
		cursor = this.cursor;
		for (var i = this.list.length - 1; i >= 0; i--) {
			if (this.list[i].selected && i == cursor) {
				cursor--;
			}
		}
	}
	if (cursor >= 0) {
		cursor = this.list[cursor];
	}
	this.list = list;
	var width = 0;
	for (var i = 0; i < list.length; i++) {
		var item = list[i];
		item.li.data("index", i);
		if (item == cursor) {
			cursor = i;
		}
		width = Math.max(width, item.width);
	}
	this.view.css({ minWidth: (this.width = width) + "px" });
	
	this.setCursor(cursor);
}
ListView.prototype.onClick = function(index, e) {
	this.onSelect(index, e);
}
ListView.prototype.onCursorMove = function(direction, e) {
	if (this.view[0].children.length == 0) {
		return;
	}
	var index = this.cursor + direction;
	if (index < 0) index = 0;
	if (index >= this.view[0].children.length) {
		index = this.view[0].children.length - 1;
	}
	this.onSelect(index, e);
}
ListView.prototype.onSelect = function(index, e) {
	if (e.shiftKey) {
		if (this.shiftFrom < 0) {
			this.shiftFrom = index;
		}
		var from = Math.min(this.shiftFrom, index);
		var to   = Math.max(this.shiftFrom, index);
		
		if (!e.ctrlKey) {
			// 선택 영역만 잡힘
			for (var i = 0; i < from; i++) {
				var item = this.list[i];
				if (item.selected) {
					item.li.removeClass("selected");
					item.selected = false;
				}
			}
			for (var i = to + 1; i < this.list.length; i++) {
				var item = this.list[i];
				if (item.selected) {
					item.li.removeClass("selected");
					item.selected = false;
				}
			}
		}
		for (var i = from; i <= to; i++) {
			var item = this.list[i];
			item.li.addClass("selected");
			item.selected = true;
		}
		
	} else {
		if (e.ctrlKey) {
			console.log(e);
			if (!e.keyCode) {
				this.select(index, true);
			}
		} else {
			this.clearSelection();
			this.select(index, false);
		}
	}
	this.setCursor(index);
}
ListView.prototype.select = function(index, toggle) {
	var item = this.list[index];
	if (item.selected) {
		if (toggle) {
			item.li.removeClass("selected");
			item.selected = false;
		}
	} else {
		item.li.addClass("selected");
		item.selected = true;
	}
	this.shiftFrom = -1;
}
ListView.prototype.setCursor = function(index) {
	if (this.list.length < 1) {
		this.cursor = -1;
		return;
	}
	if (this.cursor >= 0) {
		this.list[this.cursor].li.removeClass("cursor");
	}
	this.list[this.cursor = index].li.addClass("cursor");
}
ListView.prototype.clearSelection = function(withCursor) {
	for (var i = 0; i < this.list.length; i++) {
		var item = this.list[i];
		if (item.selected) {
			item.li.removeClass("selected");
			item.selected = false;
		}
		item.li.data("index", i);
	}
	if (withCursor) {
		if (this.cursor >= 0) {
			this.list[this.cursor].li.removeClass("cursor");
		}
		this.cursor = -1;
	}
	this.shiftFrom = -1;
}