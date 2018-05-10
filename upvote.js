var baseVoteWeight = 2500
var malusPerVote = 1 / Math.pow(2, 1/7) // 7 votes a week divides vote weight by 2
var steem = require('steem')
steem.api.setOptions({ url: 'https://api.steemit.com' })
var mysql = require('mysql')
var connection = mysql.createConnection({
	host     : 'localhost',
	user     : 'root',
	password : '',
	database : 'dtube'
})
connection.connect();

var query = 'SELECT COUNT(*) as c FROM vote WHERE author = \''+process.argv[2]+'\' AND stamp >= DATE(NOW()) - INTERVAL 7 DAY;'
connection.query(query, function (err, res, fields) {
	var c = res[0].c
	var malusVotes = 1
	if (c > 3) 
		malusVotes = Math.pow(malusPerVote, c-3)
	steem.api.getAccounts(['dtube', process.argv[2]], function(err, res) {
		if (res[0].voting_power > 8000)
			steem.api.getContent(process.argv[2], process.argv[3], function(err, res) {
				var curators = process.argv[4].split(',')
				var voteWeight = baseVoteWeight*curators.length
				voteWeight *= malusVotes
				voteWeight = 100*Math.round(voteWeight/100)
				if (voteWeight > 10000) voteWeight = 10000
				console.log(voteWeight)
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
})