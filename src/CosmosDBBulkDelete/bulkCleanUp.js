// Originally taken from:
// https://raw.githubusercontent.com/Azure/azure-cosmos-dotnet-v2/master/samples/clientside-transactions/DocDBClientBulk/DocDBClientBulk/bulkDelete.js
function bulkCleanUp(num) {
    var collection = getContext().getCollection();
    var collectionLink = collection.getSelfLink();
    var response = getContext().getResponse();
    var responseBody = {
        deleted: 0,
        continuation: true
    };

    tryQueryAndDelete()

    function tryQueryAndDelete(continuation) {
        var requestOptions = { continuation: continuation };

        var query = { query: "select * from root r" };
        var isAccepted = collection.queryDocuments(collectionLink, query, requestOptions, function (err, retrievedDocs, responseOptions) {
            if (err) throw err;

            if (retrievedDocs.length > 0) {
                // Begin deleting documents as soon as documents are returned form the query results.
                // tryDelete() resumes querying after deleting; no need to page through continuation tokens.
                //  - this is to prioritize writes over reads given timeout constraints.
                tryDelete(retrievedDocs);
            } else if (responseOptions.continuation) {
                // Else if the query came back empty, but with a continuation token; repeat the query w/ the token.
                tryQueryAndDelete(responseOptions.continuation);
            } else {
                // Else if there are no more documents and no continuation token - we are finished deleting documents.
                responseBody.continuation = false;
                response.setBody(responseBody);
            }
        });

        // If we hit execution bounds - return continuation: true.
        if (!isAccepted) {
            response.setBody(responseBody);
        }
    }

    // Recursively deletes documents passed in as an array argument.
    // Attempts to query for more on empty array.
    function tryDelete(documents) {
        if (documents.length > 0) {
            // Delete the first document in the array.
            var isAccepted = collection.deleteDocument(documents[0]._self, {}, function (err, responseOptions) {
                if (err) throw err;

                responseBody.deleted++;
                if (responseBody.deleted >= num) {
                    response.setBody(responseBody);
                }
                else {
                    documents.shift();
                    // Delete the next document in the array.
                    tryDelete(documents);
                }
            });

            // If we hit execution bounds - return continuation: true.
            if (!isAccepted) {
                response.setBody(responseBody);
            }
        } else {
            // If the document array is empty, query for more documents.
            tryQueryAndDelete();
        }
    }
}
