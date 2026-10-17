const {fail} = require('./domain');

// Callable data is untrusted even when Firebase has verified the caller.
function requestData(request, actions) {
  const data = request.data;
  if (!data || typeof data !== 'object' || Array.isArray(data) ||
      typeof data.action !== 'string' || !actions.includes(data.action))
    fail('invalid-argument', 'Choose a supported action.');
  return data;
}

module.exports = {requestData};
