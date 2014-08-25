processQuery = function(query, mapFunction) {
  var terms = query.split(" ");

  var term;
  var quoteOpen;
  var not = false;

  //Extract the terms of the query, set each terms attribute and call the callback
  //Possible modifiers: 
  //'-' before a term negates it 
  //Single or double quotes around space-seperated terms combine them into a phrase
  for (var i = 0; i < terms.length; i++) {
    // if (terms[i].trim().length === 0) continue;

    if (quoteOpen === undefined) {
      term = terms[i];
      not = false;

      //Check for negation prefix
      if (term.charAt(0) === '-') {
        term = term.substring(1, term.length);
        not = true;
      }

      //Check for opening quotes
      if (term.charAt(0) === "'" || term.charAt(0) === '"') {
        quoteOpen = term.charAt(0);
        term = term.substring(1, term.length);
      } else {
        mapFunction(term, not, false, (i < terms.length-1));
        term = undefined;
      }
    } else {
      //A phrase is still open, add the term to the existing phrase
      term += " " + terms[i];

      //Check if the phrase ends here
      if (term.charAt(term.length-1) === quoteOpen) {
        term = term.substring(0, term.length-1);
        quoteOpen = undefined;

        mapFunction(term, not, true, true);
        term = undefined;
      }
    }
  }

  //If we have an unfinished phrase open, send it as well
    if (quoteOpen !== undefined && term !== undefined) {
      mapFunction(term, not, true, false);
    }
};
  