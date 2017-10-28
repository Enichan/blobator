window.Reparagrapher = (function (customOptions) {
  // events:
  // onParagraph(element, options)

  var self = {};

  function isParagraphStart(node, options) {
    if (node.nodeName === "BR") {
      return false;
    }
    
    if (node.nodeName === "#text" && 
        node.textContent.trim().length === 0) {
      return false;
    }
    
    for (var i = 0; i < options.nonParagraphElements.length; i++) {
      if (node.nodeName === options.nonParagraphElements[i]) {
        return false;
      }
    }
      
    return true;
  }
  
  function createParagraph(content, refNode, options) {
    var node = refNode.value;
    if (node == null) {
      return false;
    }
    
    // first, find the next text node that isn't empty
    while (!isParagraphStart(node, options)) {
      if (node.nodeName === "#text" && options.removeEmptyTextNodes) {
        var remNode = node;
        node = node.nextSibling;
        content.removeChild(remNode);
      }
      else if (node.nodeName === "BR" && options.removeBrNodes) {
        var remNode = node;
        node = node.nextSibling;
        content.removeChild(remNode);
      }
      else {
        node = node.nextSibling;
      }
  
      if (node == null) {
        return false;
      }
    }
    
    // create a new P node, insert it into content, and add
    // next siblings to P node until we find a BR node
    var p = createParaEle(options);
    
    if (options.onParagraph) {
      options.onParagraph(p, options);
    }
    
    content.insertBefore(p, node);
    
    var childList = [];
    
    while (node != null && node.nodeName !== "BR") {
      childList.push(node);
      node = node.nextSibling;
    }
    
    for (var i = 0; i < childList.length; i++) {
      content.removeChild(childList[i]);
      p.appendChild(childList[i]);
    }
    
    // update reference node value
    refNode.value = node;
    return true;
  }
  
  function createParaEle(options) {
    var ele = document.createElement(options.paragraphElementType || "p");
    if (options.className) {
      ele.className = options.className;
    }
    return ele;
  }

  self.init = function(customOptions) {
    customOptions = customOptions || {};
  
    self.options = {
      "removeBrNodes": customOptions.removeBrNodes || true,
      "removeEmptyTextNodes": customOptions.removeEmptyTextNodes || true,
      "className": customOptions.className || "reparagraph",
      "nonParagraphElements": customOptions.nonParagraphElements || [ "A", "P", "DIV" ],
      "paragraphOverflowCount": customOptions.paragraphOverflowCount || 16384,
      "paragraphElementType": customOptions.paragraphElementType || "p"
    };
  
    postrender.reparagraph = function(content, taskName) {
      var options = self.options;
      var node = { "value": content.firstChild };
      
      var i = 0;
      while (createParagraph(content, node, options)) {
        i++;
        if (i > options.paragraphOverflowCount) {
          // detect infinite loop, just for safety
          break;
        }
      }
    };
  }
  
  self.init();
  
  return self;
})();