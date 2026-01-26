import { serve } from "https://deno.land/std@0.168.0/http/server.ts"

const corsHeaders = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': 'authorization, x-client-info, apikey, content-type',
}

serve(async (req) => {
  if (req.method === 'OPTIONS') {
    return new Response('ok', { headers: corsHeaders })
  }

  try {
    console.log('=== DELETE ACCOUNT REQUEST ===')
    console.log('Timestamp:', new Date().toISOString())
    
    // Afficher TOUTES les variables d'environnement
    const allEnvVars = Deno.env.toObject()
    console.log('All env vars keys:', Object.keys(allEnvVars))
    
    const authHeader = req.headers.get('Authorization')
    console.log('Authorization header present:', !!authHeader)
    
    if (!authHeader) {
      console.log('ERROR: No Authorization header')
      return new Response(
        JSON.stringify({ error: 'Missing authorization header' }),
        { status: 401, headers: { ...corsHeaders, 'Content-Type': 'application/json' } }
      )
    }

    const jwt = authHeader.replace('Bearer ', '').trim()
    console.log('JWT length:', jwt.length)
    console.log('JWT first 50 chars:', jwt.substring(0, 50))
    
    // Extraire user_id du JWT
    const parts = jwt.split('.')
    if (parts.length !== 3) {
      console.log('ERROR: JWT has wrong number of parts:', parts.length)
      return new Response(
        JSON.stringify({ error: 'Invalid JWT format' }),
        { status: 401, headers: { ...corsHeaders, 'Content-Type': 'application/json' } }
      )
    }
    
    let userId
    try {
      const payload = JSON.parse(atob(parts[1]))
      console.log('JWT decoded successfully')
      console.log('JWT sub:', payload.sub)
      console.log('JWT exp:', payload.exp, '=', new Date(payload.exp * 1000).toISOString())
      console.log('Current time:', Date.now() / 1000, '=', new Date().toISOString())
      console.log('JWT expired?', payload.exp < Date.now() / 1000)
      
      if (payload.exp < Date.now() / 1000) {
        console.log('ERROR: JWT is expired')
        return new Response(
          JSON.stringify({ error: 'JWT expired' }),
          { status: 401, headers: { ...corsHeaders, 'Content-Type': 'application/json' } }
        )
      }
      
      userId = payload.sub
    } catch (e) {
      console.log('ERROR: Cannot decode JWT:', e.message)
      return new Response(
        JSON.stringify({ error: 'Invalid JWT', details: e.message }),
        { status: 401, headers: { ...corsHeaders, 'Content-Type': 'application/json' } }
      )
    }
    
    // Vérifier les variables d'environnement
    const supabaseUrl = Deno.env.get('SUPABASE_URL')
    const serviceRoleKey = Deno.env.get('MY_SERVICE_ROLE_KEY')
    
    console.log('SUPABASE_URL:', supabaseUrl)
    console.log('SUPABASE_URL type:', typeof supabaseUrl)
    console.log('SERVICE_ROLE_KEY present:', !!serviceRoleKey)
    console.log('SERVICE_ROLE_KEY type:', typeof serviceRoleKey)
    console.log('SERVICE_ROLE_KEY length:', serviceRoleKey?.length ?? 0)
    console.log('SERVICE_ROLE_KEY first 30 chars:', serviceRoleKey?.substring(0, 30) ?? 'NULL')
    
    if (!serviceRoleKey || serviceRoleKey.length < 100) {
      console.log('ERROR: SERVICE_ROLE_KEY missing or too short')
      return new Response(
        JSON.stringify({ 
          error: 'Server misconfiguration',
          details: 'SERVICE_ROLE_KEY not available',
          availableVars: Object.keys(allEnvVars)
        }),
        { status: 500, headers: { ...corsHeaders, 'Content-Type': 'application/json' } }
      )
    }
    
    // Supprimer via API REST
    console.log('Calling admin API to delete user:', userId)
    const deleteUrl = `${supabaseUrl}/auth/v1/admin/users/${userId}`
    console.log('Delete URL:', deleteUrl)
    
    const deleteResponse = await fetch(deleteUrl, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${serviceRoleKey}`,
        'apikey': serviceRoleKey,
        'Content-Type': 'application/json'
      }
    })
    
    console.log('Delete response status:', deleteResponse.status)
    console.log('Delete response statusText:', deleteResponse.statusText)
    
    const responseBody = await deleteResponse.text()
    console.log('Delete response body:', responseBody)
    
    if (!deleteResponse.ok) {
      console.log('ERROR: Delete failed')
      return new Response(
        JSON.stringify({ 
          error: 'Failed to delete user',
          status: deleteResponse.status,
          details: responseBody
        }),
        { status: deleteResponse.status, headers: { ...corsHeaders, 'Content-Type': 'application/json' } }
      )
    }
    
    console.log('SUCCESS: User deleted')
    return new Response(
      JSON.stringify({ success: true, message: 'Account deleted', userId }),
      { status: 200, headers: { ...corsHeaders, 'Content-Type': 'application/json' } }
    )

  } catch (err) {
    console.error('FATAL ERROR:', err.message)
    console.error('Error stack:', err.stack)
    return new Response(
      JSON.stringify({ 
        error: 'Internal error',
        message: err.message,
        stack: err.stack
      }),
      { status: 500, headers: { ...corsHeaders, 'Content-Type': 'application/json' } }
    )
  }
})
