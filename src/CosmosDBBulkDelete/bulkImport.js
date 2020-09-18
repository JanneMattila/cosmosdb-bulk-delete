// Originally taken from:
// https://raw.githubusercontent.com/Azure/azure-cosmos-dotnet-v2/master/samples/clientside-transactions/DocDBClientBulk/DocDBClientBulk/bulkImport.js
function bulkImport(doc, num) {
    var collection = getContext().getCollection();
    var collectionLink = collection.getSelfLink();

    let name = doc.name;
    var count = 0;
    if (!doc) throw new Error("Invalid parameter.");

    tryCreate(doc, callback);

    function tryCreate(doc, callback) {

        doc.id = "";
        doc.name = name + count;

        var isAccepted = collection.createDocument(collectionLink, doc, callback);
        if (!isAccepted) getContext().getResponse().setBody(count);
    }

    function callback(err, doc, options) {
        if (err) throw err;

        count++;
        if (count >= num) {
            getContext().getResponse().setBody(count);
        } else {
            tryCreate(doc, callback);
        }
    }
}
