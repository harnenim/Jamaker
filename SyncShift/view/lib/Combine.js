var Combine = {
	css: 'font-family: 맑은 고딕;'
};
{
	var LINE = {
			TEXT: 0
		,	SYNC: 1
		,	TYPE: 2
	};
	var TYPE = {
			TEXT: null
		,	BASIC: 1
		,	FRAME: 2
		,	RANGE: 3
	};
	
	var STIME = 0;
	var STYPE = 1;
	var ETIME = 2;
	var ETYPE = 3;
	var TEXT  = 4;
	var LINES = 5;
	var WIDTH = 6;
	var UPPER = 4;
	var LOWER = 5;
	
	var LOG = false;
	
	function getWidth(smi, checker) {
		// RUBY태그 문법이 미묘하게 달라서 가공 필요
		smi = smi.split("<RP").join("<!--RP").split("</RP>").join("</RP-->");
		
		// 태그 밖의 공백문자 치환
		var tags = smi.split("<");
		for (var i = 1; i < tags.length; i++) {
			var index = tags[i].indexOf(">");
			if (index > 0) {
				tags[i] = tags[i].substring(0, index) + tags[i].substring(index);
			}
		}
		smi = tags.join("<");
		
		var lines = smi.split(/<br>/gi);
		for (var i = 0; i < lines.length; i++) {
			lines[i] = checker.html(lines[i]).text();
		}
		return checker.text(lines.join("\n")).width();
	}
	function getChecker() {
		if (!Combine.checker) {
			$("body").append(Combine.checker = $("<span>"));
		}
		Combine.checker.attr({ style: Combine.css }).css({
				whiteSpace: "pre"
			,	fontSize: "144px"
			,	fontWeight: "bold"
			,	textShadow: ""
		});
		return Combine.checker.show();
	}
	
	var syncLines = { basic: {}, frame: {}, range: {} };
	function getSyncLine(sync, type) {
		var line = null;
		switch (type) {
			case TYPE.FRAME:
				line = syncLines.frame[sync];
				break;
			case TYPE.RANGE:
				line = syncLines.range[sync];
				break;
			default:
				line = syncLines.basic[sync];
		}
		/*
		if (line == null) {
			// 여기 올 일은 없어야 함
			line = SmiEditor.makeSyncLine(sync, type);
		}
		*/
		return line;
	}
	
	function parse(text, checker) {
		var lines = text.split("\n");
		var parseds = [];
		
		// 싱크값을 제외하면 별도의 값을 취하지 않는 간이 파싱
		// SMI는 태그 꺽쇠 내에서 줄바꿈을 하는 경우는 일반적으로 없다고 가정
		for (var i = 0; i < lines.length; i++) {
			var line = lines[i];
			var parsed = [line, null, null];
			parseds.push(parsed);
			
			var j = 0;
			var k = 0;
			var hasSync = false;
			var sync = 0;
			
			while ((k = line.indexOf("<", j)) >= 0) {
				// 태그 열기
				j = k + 1;

				// 태그 닫힌 곳까지 탐색
				var closePos = line.indexOf(">", j);
				if (j < closePos) {
					// 태그명 찾기
					for (k = j; k < closePos; k++) {
						var c = line[k];
						if (c == ' ' || c == '\t' || c == '"' || c == "'" || c == '\n') {
							break;
						}
					}
					var tagName = line.substring(j, k);
					j = k;
					
					hasSync = (tagName.toUpperCase() == "SYNC");

					if (hasSync) {
						while (j < closePos) {
							// 속성 찾기
							for (; j < closePos; j++) {
								var c = line[j];
								if (('0'<=c&&c<='9') || ('a'<=c&&c<='z') || ('A'<=c&&c<='Z')) {
									break;
								}
								//html += c;
							}
							for (k = j; k < closePos; k++) {
								var c = line[k];
								if ((c<'0'||'9'<c) && (c<'a'||'z'<c) && (c<'A'||'Z'<c)) {
									break;
								}
							}
							var attrName = line.substring(j, k);
							j = k;
							
							// 속성 값 찾기
							if (line[j] == "=") {
								j++;
								
								var q = line[j];
								if (q == "'" || q == '"') { // 따옴표로 묶인 경우
									k = line.indexOf(q, j + 1);
									k = (0 <= k && k < closePos) ? k : closePos;
								} else {
									q = "";
									k = line.indexOf(" ");
									k = (0 <= k && k < closePos) ? k : closePos;
									k = line.indexOf("\t");
									k = (0 <= k && k < closePos) ? k : closePos;
								}
								var value = line.substring(j + q.length, k);
								
								if (q.length && k < closePos) { // 닫는 따옴표가 있을 경우
									j += q.length + value.length + q.length;
								} else {
									j += q.length + value.length;
								}
								
								if (attrName.toUpperCase() == "START" && isFinite(value)) {
									sync = Number(value);
								}
							}
						}
					} else {
						// 싱크 태그 아니면 그냥 제낌
						j = closePos;
					}
					
					// 태그 닫기
					j++;
				}
			}
			
			if (parsed[LINE.SYNC] = sync) { // 어차피 0이면 플레이어에서도 씹힘
				// 화면 싱크 체크
				parsed[LINE.TYPE] = TYPE.BASIC;
				var typeCss = "";
				if (line.indexOf(" >") > 0) {
					parsed[LINE.TYPE] = TYPE.FRAME;
					typeCss = " frame";
					syncLines.frame[sync] = line;
				} else if (line.indexOf("\t>") > 0) {
					parsed[LINE.TYPE] = TYPE.RANGE;
					typeCss = " range";
					syncLines.range[sync] = line;
				} else {
					syncLines.basic[sync] = line;
				}
			} else {
				parsed[LINE.TYPE] = TYPE.TEXT;
			}
		}
		parseds.push(["&nbsp;", 99999999, TYPE.BASIC]);
		
		var syncs = [];
		var last = null;
		for (var i = 0; i < parseds.length; i++) {
			var parsed = parseds[i];
			if (parsed[LINE.TYPE]) {
				if (last) {
					var text = [];
					for (var j = last[0] + 1; j < i; j++) {
						text.push(parseds[j][LINE.TEXT]);
					}
					text = text.join("\n");
					if (text.split("&nbsp;").join("").trim()) {
						var lineCount = text.split(/<br>/gi).length;
						//[STIME, STYPE, ETIME, ETYPE, TEXT, LINES, WIDTH];
						syncs.push([last[LINE.SYNC], last[LINE.TYPE], parsed[LINE.SYNC], parsed[LINE.TYPE], text, lineCount, getWidth(text, checker)]);
					}
				}
				last = [i, parsed[LINE.SYNC], parsed[LINE.TYPE]];
			}
		}
		
		return syncs;
	}
	
	Combine.combine = function(inputUpper, inputLower) {
		var hljs = $(".hljs").hide(); // 결합 로직 돌아갈 때 문법 하이라이트가 있으면 성능 저하됨
		var checker = getChecker();
		var upperSyncs = parse(inputUpper, checker);
		var lowerSyncs = parse(inputLower, checker);
		
		var ui = 0;
		var li = 0;
		var groups = [];
		var group = null;
		while  ((ui <= upperSyncs.length) && (li <= lowerSyncs.length)) {
			if ((ui == upperSyncs.length) && (li == lowerSyncs.length)) {
				break;
			}
			var us = (ui < upperSyncs.length) ? upperSyncs[ui] : [99999999, 99999999, null, 0];
			var ls = (li < lowerSyncs.length) ? lowerSyncs[li] : [99999999, 99999999, null, 0];
			if (us[STIME] < ls[STIME]) { // 위가 바뀜
				if ((us[STYPE] == TYPE.RANGE) // 중간 싱크
				 || (group && group.lower.length && (group.lower[group.lower.length - 1][ETIME] > us[STIME]))
				){ // 그룹 유지
					group.upper.push(us);
					group.maxLines[0] = Math.max(group.maxLines[0], us[LINES]);
					group.maxWidth = Math.max(group.maxWidth, us[WIDTH]);
					
				} else { // 아래가 없거나 끝남 -> 그룹 끊김
					groups.push(group = {
							upper: [us]
						,	lower: []
						,	maxLines: [us[LINES], 0]
						,	maxWidth: us[WIDTH]
					});
				}
				ui++;
				
			} else if (ls[STIME] < us[STIME]) { // 아래가 바뀜
				if ((ls[STYPE] == TYPE.RANGE) // 중간 싱크
				 || (group && group.upper.length && (group.upper[group.upper.length - 1][ETIME] > ls[STIME]))
				) { // 그룹 유지
					group.lower.push(ls);
					group.maxLines[1] = Math.max(group.maxLines[1], ls[LINES]);
					group.maxWidth = Math.max(group.maxWidth, ls[WIDTH]);
					
				} else { // 위가 없거나 끝남 -> 그룹 끊김
					groups.push(group = {
							upper: []
						,	lower: [ls]
						,	maxLines: [0, ls[LINES]]
						,	maxWidth: ls[WIDTH]
					});
				}
				li++;
				
			} else { // 둘이 같이 바뀜
				if ((us[STYPE] == TYPE.RANGE) || (ls[STYPE] == TYPE.RANGE)) {
					// 하나라도 중간 싱크 - 그룹 유지
					group.upper.push(us);
					group.lower.push(ls);
					group.maxLines[0] = Math.max(group.maxLines[0], us[LINES]);
					group.maxLines[1] = Math.max(group.maxLines[1], ls[LINES]);
					group.maxWidth = Math.max(group.maxWidth, us[WIDTH]);
					group.maxWidth = Math.max(group.maxWidth, ls[WIDTH]);
					
				} else {
					// 새 그룹
					groups.push(group = {
							upper: [us]
						,	lower: [ls]
						,	maxLines: [us[LINES], ls[LINES]]
						,	maxWidth: Math.max(us[WIDTH], ls[WIDTH])
					});
				}
				ui++;
				li++;
			}
		}

		for (var gi = 0; gi < groups.length; gi++) {
			var group = groups[gi];
			group.lines = [];
			var last = null;
			
			if (LOG) console.log("group width: " + group.maxWidth);
			
			// 팟플레이어 왼쪽 정렬에서 좌우로 흔들리지 않도록 잡아줌
			// ... 사실 폰트에 따라 흔들리긴 함...
			var lists = [group.upper, group.lower];
			for (var i = 0; i < lists.length; i++) {
				var list = lists[i];
				
				for (var j = 0; j < list.length; j++) {
					// 줄 길이 채워주기
					var sync = list[j];
					if (sync[WIDTH] < group.maxWidth) {
						var line = sync[TEXT];
						var lines = line.split(/<br>/gi);
						
						// 여러 줄일 경우 제일 긴 줄 찾기
						if (lines.length > 1) {
							var maxWidth = 0;
							for (var k = 0; k < lines.length; k++) {
								var width = getWidth(lines[k], checker);
								if (width > maxWidth) {
									maxWidth = width;
									line = lines[k];
								}
							}
						}
						
						// 여백을 붙여서 제일 적절한 값 찾기
						var pad = "";
						var width = getWidth(line, checker);
						var lastPad;
						var lastWidth;
						if (LOG) console.log(line.split("&nbsp;").join(" ") + ": " + width);
						do {
							lastPad = pad;
							lastWidth = width;
							pad = lastPad + " ";
							var curr = "​" + pad + line + pad + "​";
							width = getWidth(curr, checker);
							if (LOG) console.log(curr.split("&nbsp;").join(" ") + ": " + width);
							
						} while (width < group.maxWidth);
						
						if ((width - group.maxWidth) > (group.maxWidth - lastWidth)) {
							pad = lastPad;
							if (LOG) {
								var curr = "​" + pad + line + pad + "​";
								width = getWidth(curr, checker);
							}
						}
						pad = pad.split("&nbsp;").join(" ");
						for (var k = 0; k < lines.length; k++) {
							var line = lines[k].split("​").join(""); // Zero-Width-Space 중복으로 들어가지 않도록
							
							if ($("<span>").html(line).text().split("　").join("").split(" ").join("").length) {
								// 공백 줄인 경우는 별도 처리 하지 않음
								// 태그로 감싼 줄은 태그 안에 공백문자 넣기
								var prev = "", next = "";
								while (line.startsWith("\n")) {
									prev += "\n";
									line = line.substring(1);
								}
								while (line.startsWith("<")) {
									var tagEnd = line.indexOf(">") + 1;
									if (tagEnd == 0) {
										break;
									}
									while (line.length > tagEnd && line[tagEnd] == "\n") {
										// 태그 직후에 줄바꿈을 한 경우가 있음
										tagEnd++;
									}
									prev += line.substring(0, tagEnd);
									line = line.substring(tagEnd);
								}
								while (line.endsWith(">")) {
									var tagStart = line.lastIndexOf("<");
									if (tagStart < 0) {
										break;
									}
									next = line.substring(tagStart) + next;
									line = line.substring(0, tagStart);
								}
								line = prev + "​" + pad + line + pad + "​" + next;
								lines[k] = line;
							}
						}
						sync[TEXT] = lines.join("<br>");
					}
					
					// 줄 높이 맞춰주기
					for (var k = sync[LINES]; k < group.maxLines[i]; k++) {
						sync[TEXT] = "<b>　</b><br>" + sync[TEXT];
					}
				}
			}
			
			ui = li = 0;
			while  ((ui <= group.upper.length) && (li <= group.lower.length)) {
				if ((ui == group.upper.length) && (li == group.lower.length)) {
					break;
				}
				var us = (ui < group.upper.length) ? group.upper[ui] : [99999999, 99999999, null, 0];
				var ls = (li < group.lower.length) ? group.lower[li] : [99999999, 99999999, null, 0];
				
				if (us[STIME] < ls[STIME]) { // 위가 바뀜
					if (!last) { // 첫 싱크
						group.lines.push(last = [us[STIME], us[STYPE], us[ETIME], us[ETYPE], us, null]);
						
					} else {
						// 아래는 유지하고 위는 바뀐 걸 추가
						if (last[STIME] == us[STIME]) {
							last[UPPER] = us;
						} else {
							var curr = [us[STIME], us[STYPE], last[2], last[3], us, last[LOWER]];
							last[ETIME] = us[STIME];
							last[ETYPE] = us[STYPE];
							group.lines.push(last = curr);
						}
						
						if (us[ETIME] < last[ETIME]) { // 위가 먼저 끝남
							var curr = [us[ETIME], us[ETYPE], last[ETIME], last[ETYPE], null, last[LOWER]];
							last[ETIME] = us[ETIME];
							last[ETYPE] = us[ETYPE];
							group.lines.push(last = curr);
						} else if (us[ETIME] > last[ETIME]) { // 아래가 먼저 끝남
							group.lines.push(last = [last[ETIME], last[ETYPE], us[ETIME], us[ETYPE], us, null]);
						} else {
							// 둘 다 끝남 -> 그룹 끝
						}
					}
					ui++;
					
				} else if (ls[STIME] < us[STIME]) { // 아래가 바뀜
					if (!last) { // 첫 싱크
						group.lines.push(last = [ls[STIME], ls[STYPE], ls[ETIME], ls[ETYPE], null, ls]);
						
					} else {
						// 위는 유지하고 아래는 바뀐 걸 추가
						if (last[STIME] == ls[STIME]) {
							last[LOWER] = ls;
						} else {
							var curr = [ls[STIME], ls[STYPE], last[ETIME], last[ETYPE], last[UPPER], ls];
							last[ETIME] = ls[STIME];
							last[ETYPE] = ls[STYPE];
							group.lines.push(last = curr);
						}
						
						if (ls[ETIME] < last[ETIME]) { // 아래가 먼저 끝남
							var curr = [ls[ETIME], ls[ETYPE], last[ETIME], last[ETYPE], last[TEXT], null];
							last[ETIME] = ls[ETIME];
							last[ETYPE] = ls[ETYPE];
							group.lines.push(last = curr);
						} else if (ls[ETIME] > last[ETIME]) { // 위가 먼저 끝남
							group.lines.push(last = [last[ETIME], last[ETYPE], ls[ETIME], ls[ETYPE], null, ls]);
						} else {
							// 둘 다 끝남 -> 그룹 끝
						}
					}
					li++;
					
				} else { // 둘이 같이 바뀜(그룹 첫 싱크에서만 가능)
					var ss = us;
					if (ls[ETIME] < us[ETIME]) {
						ss = ls;
						li++;
					} else {
						ui++;
					}
					group.lines.push(last = [us[STIME], us[STYPE], ss[ETIME], ss[ETYPE], us, ls]);
				}
			}
		}
		checker.text("").hide();
		hljs.show();
		
		var lines = [];
		var lastSync = 0;
		for (var gi = 0; gi < groups.length; gi++) {
			var group = groups[gi];
			var forEmpty = [[], []];
			for (var i = 0; i < 2; i++) {
				for (var j = 0; j < group.maxLines[i]; j++) {
					forEmpty[i].push("<b>　</b>");
				}
				forEmpty[i] = forEmpty[i].join("<br>");
			}
			
			for (var i = 0; i < group.lines.length; i++) {
				var line = group.lines[i];
				
				if (lastSync < line[STIME]) {
					if (gi > 0) { // 처음일 땐 제외
						lines.push("&nbsp;");
					}
					lines.push(getSyncLine(line[STIME], line[STYPE]));
				}
				if (group.upper.length == 0) {
					lines.push(line[LOWER] ? line[LOWER][TEXT] : "&nbsp;");
				} else if (group.lower.length == 0) {
					lines.push(line[UPPER] ? line[UPPER][TEXT] : "&nbsp;");
				} else {
					lines.push((line[UPPER] ? line[UPPER][TEXT] : forEmpty[0]) + "<br>" + (line[LOWER] ? line[LOWER][TEXT] : forEmpty[1]));
				}
				if (line[ETIME] < 99999999) {
					lines.push(getSyncLine(lastSync = line[ETIME], line[ETYPE]));
				} else {
					lastSync = 0;
				}
			}
		}
		if (lastSync) {
			lines.push("&nbsp;");
		}
		return lines;
	}
}

if (Subtitle && Subtitle.SmiFile) {
	Subtitle.SmiFile.textToHolds = function(text) {
		var texts = text.split("\r\n").join("\n").split("\n<!-- Hold=");
		var holds = [{ text: texts[0] }];
		for (var i = 1; i < texts.length; i++) {
			var hold = texts[i];
			var begin = hold.indexOf("\n");
			var end = hold.indexOf("-->");
			if (begin < 0 || end < 0) {
				holds[0].text += "\n<!-- Hold=" + hold;
				continue;
			}
			// Hold 내용물 뒤에 뭐가 더 붙어있을 경우
			if (end < hold.length - 3) {
				holds[0].text += hold.substring(end);
			}
			var name = hold.substring(0, begin).trim();
			var pos = 1;
			var index = name.indexOf("|");
			if (index) {
				try {
					pos = Number(name.substring(0, index));
				} catch (e) {
					console.log(e);
				}
				name = name.substring(index + 1);
			}
			holds.push({
					pos: pos
				,	name: name
				,	text: hold.substring(begin, end).trim().split("<​").join("<").split("​>").join(">")
			});
		}
		
		// SMI 파일 역정규화
		var normalized = new Subtitle.SmiFile(holds[0].text).antiNormalize();
		normalized[0].pos = 0;
		normalized[0].name = "메인";
		holds = normalized.concat(holds.slice(1));
		holds[0].text = holds[0].toTxt().trim();
		for (var i = 1; i < normalized.length; i++) {
			// 내포된 홀드는 종료싱크가 빠졌을 수 있음
			var hold = holds[i];
			if (hold.next && hold.body[hold.body.length - 1].text.split("&nbsp;").join("").trim().length > 0) {
				hold.body.push(new Subtitle.Smi(hold.next.start, hold.next.syncType, "&nbsp;"));
			}
			holds[i].text = hold.toTxt().trim();
		}
		for (var i = normalized.length; i < holds.length; i++) {
			holds[i].text = new Subtitle.SmiFile(holds[i].text).antiNormalize()[0].toTxt().trim();
		}
		return holds;
	}
	Subtitle.SmiFile.holdsToText = function(origHolds, withNormalize=true, withCombine=true, withComment=true) {
		var result = [];
		var logs = [];
		var originBody = [];
		
		var main = new Subtitle.SmiFile(origHolds[0].text);
		withCombine = withCombine && origHolds.length > 1;
		
		// 정규화 등 작업
		if (withNormalize) {
			var normalized = Subtitle.Smi.normalize(main.body, withComment && !withCombine);
			originBody = normalized.origin;
			logs = normalized.logs;
		} else {
			if (origHolds.length > 1) {
				originBody = main.body.slice(0, main.body.length);
			}
		}
		
		if (withCombine) {
			// 시작 시간 순으로 저장
			var holds = origHolds.slice(1);
			holds.sort(function(a, b) {
				return a.start - b.start;
			});
			for (var hi = 0; hi < holds.length; hi++) {
				var hold = holds[hi];
				result[hold.resultIndex = (hi + 1)] = "<!-- Hold=" + hold.pos + "|" + hold.name + "\n" + hold.text.split("<").join("<​").split(">").join("​>") + "\n-->";
			}
			
			// 메인에 가까운 걸 먼저 작업해야 함
			// 단, 아래쪽부터 쌓아야 함
			holds = origHolds.slice(0);
			holds.sort(function(a, b) {
				var aPos = a.viewPos;
				var bPos = b.viewPos;
				if (aPos < 0) {
					if (bPos > 0) {
						return -1;
					}
				} else {
					if (bPos < 0) {
						return 1;
					}
				}
				if (aPos < 0) aPos = -aPos;
				if (bPos < 0) bPos = -bPos;
				if (aPos < bPos) return -1;
				if (aPos > bPos) return 1;
				return 0;
			});
			
			var holdSmis = [];
			for (var hi = 1; hi < holds.length; hi++) {
				var hold = holds[hi];
				var smi = holdSmis[hi] = new Subtitle.SmiFile(hold.text);
				smi.header = smi.footer = "";
				if (withNormalize) {
					Subtitle.Smi.normalize(smi.body, false);
				}
				
				if (smi.body.length == 0) {
					continue;
				}
				
				var start = smi.body[0].start;
				var end = smi.body[smi.body.length - 1].start;
				
				// 메인에서 홀드와 겹치는 영역 찾기
				var mainBegin = 0;
				for (var i = 0; i <= main.body.length; i++) {
					if (i == main.body.length) {
						mainBegin = i;
						break;
					}
					if (main.body[i].start >= start) {
						break;
					}
					mainBegin = i;
				}
				if (mainBegin == main.body.length) {
					// 홀드 전체가 메인보다 뒤에 있음
					main.body = main.body.concat(smi.body);
					continue;
				}
				if (main.body[mainBegin].text.split("&nbsp;").join("").trim().length == 0) {
					mainBegin++;
				}
				
				var mainEnd = mainBegin;
				for (; mainEnd < main.body.length; mainEnd++) {
					if (main.body[mainEnd].start > end) {
						break;
					}
				}
				if (mainEnd == 0) {
					// 홀드 전체가 메인보다 앞에 있음
					main.body = smi.body.concat(main.body);
					continue;
				}
				
				// 홀드 결합
				var sliced = new Subtitle.SmiFile();
				sliced.body = main.body.slice(mainBegin, mainEnd);
				
				var slicedText = sliced.toTxt().trim();
				var combineText = smi.toTxt().trim();
				var combined = new Subtitle.SmiFile(((hold.pos < 0) ? Combine.combine(slicedText, combineText) : Combine.combine(combineText, slicedText)).join("\n"));
				// 원칙상 normalized.result를 다뤄야 맞을 것 같지만...
				end = (mainEnd < main.body.length) ? main.body[mainEnd].start : 999999999;
				main.body = main.body.slice(0, mainBegin).concat(combined.body).concat(main.body.slice(mainEnd));
			}
			
			if (withComment) {
				if (withCombine) {
					// 홀드 결합 있을 경우 주석처리 재계산
					logs = [];
					var oi = 0;
					var ni = 0;
					
					while ((oi < originBody.length) && (ni < main.body.length)) {
						if ((originBody[oi].start == main.body[ni].start)
						 && (originBody[oi].text  == main.body[ni].text )) {
							oi++;
							ni++;
							continue;
						}
						
						// 변환결과가 원본과 동일하지 않은 범위 찾기
						var newLog = {
								from: [oi, originBody.length]
							,	to  : [ni, main.body.length]
							,	start: main.body[ni].start
							,	end: 999999999
						};
						while ((oi < originBody.length) && (ni < main.body.length)) {
							if (originBody[oi].start < main.body[ni].start) {
								oi++;
								continue;
							}
							if (originBody[oi].start > main.body[ni].start) {
								ni++;
								continue;
							}
							if (originBody[oi].text != main.body[ni].text) {
								oi++;
								ni++;
								continue;
							}
							// 싱크-내용 모두 동일한 곳 찾음
							newLog.from[1] = oi;
							newLog.to  [1] = ni;
							newLog.end = main.body[ni].start;
							break;
						}
						logs.push(newLog);
					}
					// 메인 홀드에 없는 내용만 남음
					if (ni < main.body.length) {
						var newLog = {
								from: [oi, oi]
							,	to  : [ni, main.body.length]
							,	start: main.body[ni].start
							,	end  : main.body[main.body.length - 1].start + 1
						};
						logs.push(newLog);
					}
				}
				
				var origin = new Subtitle.SmiFile();
				for (var i = 0; i < logs.length; i++) {
					var log = logs[i];
					if (!log.end) {
						if (log.from[1] < originBody.length - 1) {
							log.end = originBody[log.from[1]].start;
						} else  {
							log.end = 999999999;
						}
					}
					origin.body = originBody.slice(log.from[0], log.from[1]);
					var comment = origin.toTxt().trim();
					
					// 메인 홀드 내용이 없으면 다른 홀드가 통째로 들어왔는지 확인
					if (comment.length == 0) {
						var start = log.to[0];
						for (var hi = 1; hi < holds.length; hi++) {
							var smi = holdSmis[hi];
							var isImported = true;
							for (var j = 0; j < smi.body.length; j++) {
								if (smi.body[j].start != main.body[start+j].start) {
									isImported = false;
									break;
								}
								if (smi.body[j].text != main.body[start+j].text) {
									if (j == smi.body.length - 1) {
										// 마지막 싱크일 경우 공백이면 통과시키기
										if (smi.body[j].text.split("&nbsp;").join("").trim().length == 0) {
											continue;
										}
									}
									isImported = false;
									break;
								}
							}
							if (isImported) {
								var hold = holds[hi];
								comment = "Hold=" + hold.pos + "|" + hold.name;
								result[hold.resultIndex] = "";
								if (smi.body.length < (log.to[1] - log.to[0])) {
									// 현재 홀드가 내포 홀드의 일부일 경우 나머지 구간 분할
									var nextLog = {
											from: log.from
										,	to  : [log.to[0] + smi.body.length, log.to[1]]
										,	end : log.end
									};
									log.end = nextLog.start = main.body[nextLog.to[0]].start;
									logs = logs.slice(0, i).concat([log, nextLog]).concat(logs.slice(i + 1));
								}
								
								if (withComment) {
									for (var j = 0; j < smi.body.length; j++) {
										var sync = smi.body[j];
										if (sync.comment) {
											main.body[start + j].text = sync.comment + "\n" + sync.text;
										}
									}
								}
								break;
							}
						}
					}
					main.body[log.to[0]].text = "<!-- End=" + log.end + "\n" + (comment.split("<").join("<​").split(">").join("​>")) + "\n-->\n" + main.body[log.to[0]].text;
				}
			}
		}
		result[0] = main.toTxt();
		for (var i = 1; i < result.length; i++) {
			if (result[i].length == 0) {
				result.splice(i--, 1);
			}
		}
		return withComment ? result.join("\n") : result[0];
	}
}