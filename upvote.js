var baseVoteWeight = 1500
var steem = require('steem')
steem.api.setOptions({ url: 'https://api.steemit.com' })

steem.api.getAccounts(['dtube', process.argv[2]], function(err, res) {
	if (res[0].voting_power > 80)
		steem.api.getContent(process.argv[2], process.argv[3], function(err, res) {
			// todo logic calculate voteWeight
			var curators = process.argv[4].split(',')
			var voteWeight = baseVoteWeight*curators.length
			if (voteWeight > 10000) voteWeight = 10000
			steem.broadcast.vote(
				'xxx',
				'curator',
				process.argv[2],
				process.argv[3],
				voteWeight,
				function(err, result) {
				if (err) throw err;
			});
		})
})
