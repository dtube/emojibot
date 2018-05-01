var full_vote_dollar = 550
var steem = require('steem')
steem.api.setOptions({ url: 'https://api.steemit.com' })
steem.api.getContent(process.argv[2], process.argv[3], function(err, res) {
    var voteweight = Math.floor(10000 * parseFloat(res.pending_payout_value.split(' ')[0]) / full_vote_dollar)
    steem.broadcast.vote(
    	'xxx',
    	'curator',
    	process.argv[2],
    	process.argv[3],
    	-voteweight,
    	function(err, result) {
    	if (err) throw err;
    });
})
