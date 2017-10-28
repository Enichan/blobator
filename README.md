# Twine Reparagrapher
Wraps blocks of text in Twine in &lt;p&gt; (paragraph) HTML elements automatically by hooking into postrender, going through all nodes inside the new passage, and adding &lt;p&gt;&lt;/p&gt; tags around non-empty blocks of text followed by a &lt;br&gt; tag.

Tested with SugarCube 2.18.0 in Twine 2.1.3.

## Usage

Copy the contents of twine_reparagrapher.js into your story's javascript.

## Example

**Raw passage text:**

> Lorem ipsum dolor sit amet, consectetur adipiscing elit.
> 
> Duis ac lectus nunc. Ut porta vestibulum placerat.
> 
> Curabitur ullamcorper urna sed magna rhoncus, in accumsan ante congue.

**Normal HTML when displayed:**

> Lorem ipsum dolor sit amet, consectetur adipiscing elit.&lt;br&gt;  
> &lt;br&gt;  
> Duis ac lectus nunc. Ut porta vestibulum placerat.&lt;br&gt;  
> &lt;br&gt;  
> Curabitur ullamcorper urna sed magna rhoncus, in accumsan ante congue.&lt;br&gt;

**HTML after reparagrapher:**

> &lt;p&gt;Lorem ipsum dolor sit amet, consectetur adipiscing elit.&lt;/p&gt;  
> &lt;p&gt;Duis ac lectus nunc. Ut porta vestibulum placerat.&lt;/p&gt;  
> &lt;p&gt;Curabitur ullamcorper urna sed magna rhoncus, in accumsan ante congue.&lt;/p&gt;  

## Options

This script creates a global Reparagrapher object. The Reparagrapher.options property can be used to modify the following options:

* __removeBrNodes__: removes all BR elements, defaults to true
* __removeEmptyTextNodes__: removes text nodes that contain only whitespace, defaults to true
* __className__: CSS class name to add to paragraph elements for styling, defaults to 'reparagraph'
* __nonParagraphElements__: array of HTML elements that are not wrapped, defaults to "A", "P", and "DIV" (elements should generally be in caps)
* __paragraphOverflowCount__: in case of bugs, prevents infinite loops and paragraphs, taking up all memory and crashing the user's system, defaults to 16384
* __paragraphElementType__: the element that is created for a paragraph using document.createElement, defaults to "p"

You can also change multiple options at once by calling Reparagrapher.init with an object containing options you wish to change, for example:

```javascript
Reparagrapher.init({
  "className": "textBox",
  "paragraphElementType": "div",
  "nonParagraphElements": [ "P", "DIV" ]
});
```

## Events

Reparagrapher has the following events you can set.

### onParagraph(element, options)

Called after a paragraph is created but before it is added to the document.

__Arguments__:

* element: the paragraph element that was created
* options: the current reparagraphing options

__Return value__:  
The element to be added to the document, or no return value
